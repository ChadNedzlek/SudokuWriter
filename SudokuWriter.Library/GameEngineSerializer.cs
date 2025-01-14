using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SudokuWriter.Library;

public class GameEngineSerializer
{
    private readonly List<RuleMetadata> _rules = [];

    public GameEngineSerializer()
    {
        AddRulesFromAssembly(typeof(BasicGameRule).Assembly);
    }

    public GameEngineSerializer(params IEnumerable<Type> rules)
    {
        _rules.AddRange(rules.Select(GetMetadata).Where(x => x is not null));
    }

    public void AddRulesFromAssembly(Assembly assembly)
    {
        foreach (Type type in assembly.GetTypes())
            if (GetMetadata(type) is { } metadata)
                _rules.Add(metadata);
    }

    public void AddRuleType<T>()
        where T : IGameRule
    {
        AddRuleType(typeof(T));
    }

    public void AddRuleType(Type ruleType)
    {
        RuleMetadata metadata = GetMetadata(ruleType) ?? throw new ArgumentException($"Type {ruleType.Name} is is not a valid IGameRule");
        _rules.Add(metadata);
    }

    private static RuleMetadata GetMetadata(Type type)
    {
        if (type.GetCustomAttribute<GameRuleAttribute>()?.Name is not { } name) return null;

        if (!type.IsAssignableTo(typeof(IGameRule))) return null;

        InterfaceMapping interfaceMap = type.GetInterfaceMap(typeof(IGameRule));
        int index = interfaceMap
            .InterfaceMethods.Index()
            .First(x => x.Item.Name == nameof(IGameRule.FromJsonObject))
            .Index;

        MethodInfo create = interfaceMap.TargetMethods[index];

        return new RuleMetadata(name, type, create.CreateDelegate<Func<JsonObject, IGameRule>>());
    }

    public async Task<GameEngine> LoadGameAsync(Stream source)
    {
        JsonNode root = await JsonNode.ParseAsync(source) ?? throw new InvalidDataException();
        var rows = GetOrThrow<int>(root, "rows");
        var columns = GetOrThrow<int>(root, "columns");
        var digits = GetOrThrow<int>(root, "digits");
        var boxRows = GetOrThrow<int>(root, "boxRows");
        var boxColumns = GetOrThrow<int>(root, "boxColumns");

        var structure = new GameStructure(rows, columns, digits, boxRows, boxColumns);
        CellsBuilder cells = Cells.CreateFilled(rows, columns, digits).ToBuilder();
        if (root["cells"] is JsonArray cellArray)
            foreach (JsonNode c in cellArray)
                if (c is JsonArray { Count: 3 } a)
                    cells.SetSingle(a[0].GetValue<int>(), a[1].GetValue<int>(), a[2].GetValue<ushort>());

        List<IGameRule> rules = [];
        if (root["rules"] is JsonArray ruleArray)
        {
            foreach (JsonNode r in ruleArray)
            {
                if (r is JsonArray { Count: 2 } a)
                {
                    var ruleName = r[0].GetValue<string>();
                    if (_rules.FirstOrDefault(m => m.Name.Equals(ruleName)) is not {} rule)
                    {
                        throw new InvalidDataException($"Unknown rule type {ruleName} in file");
                    }
                    JsonObject jsonObject = r[1].AsObject();
                    rules.Add(rule.Create(jsonObject));
                }
            }
        }

        if (rules.Count == 0)
        {
            rules.Add(BasicGameRule.Instance);
        }

        var state = new GameState(cells.MoveToImmutable(), structure);

        return new GameEngine(state, rules);
    }

    private T GetOrThrow<T>(JsonNode node, string key)
    {
        JsonNode valueNode = node[key];
        if (valueNode is null) throw new InvalidDataException($"Missing property '{key}'");

        var value = valueNode.GetValue<T>();
        if (value is null) throw new InvalidDataException($"Missing property '{key}'");

        return value;
    }

    public async Task SaveGameAsync(GameEngine game, Stream destination)
    {
        JsonObject root = new();
        root["rows"] = game.InitialState.Structure.Rows;
        root["columns"] = game.InitialState.Structure.Columns;
        root["digits"] = game.InitialState.Structure.Digits;
        root["boxRows"] = game.InitialState.Structure.BoxRows;
        root["boxColumns"] = game.InitialState.Structure.BoxColumns;
        JsonArray cells = new();
        for (var r = 0; r < game.InitialState.Cells.Rows; r++)
        for (var c = 0; c < game.InitialState.Cells.Columns; c++)
        {
            int single = game.InitialState.Cells.GetSingle(r, c);
            if (single != -1) cells.Add(new JsonArray(r, c, single));
        }

        root["cells"] = cells;

        JsonArray rules = new();
        foreach (IGameRule rule in game.Rules)
        {
            string ruleName = rule.GetType().GetCustomAttribute<GameRuleAttribute>()?.Name;
            if (ruleName is null) throw new ArgumentException($"Unknown game rule {rule.GetType().Name}", nameof(game));

            rules.Add(new JsonArray(ruleName, rule.ToJsonObject()));
        }

        root["rules"] = rules;

        await using var writer = new Utf8JsonWriter(destination);
        root.WriteTo(writer);
    }

    private record RuleMetadata(string Name, Type Type, Func<JsonObject, IGameRule> Create);
}