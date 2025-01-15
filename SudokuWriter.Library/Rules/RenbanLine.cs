using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json.Nodes;

namespace SudokuWriter.Library.Rules;

[GameRule("renban")]
public class RenbanLine : LineRule<RenbanLine>, ILineRule<RenbanLine>
{
    public RenbanLine(ImmutableList<LineRuleSegment> segments) : base(segments)
    {
    }
    
    public static IGameRule Create(ImmutableList<LineRuleSegment> parts, JsonObject jsonObject) => new RenbanLine(parts);
    
    public override GameResult Evaluate(GameState state)
    {
        throw new NotImplementedException();
    }

    public override GameState? TryReduce(GameState state)
    {
        throw new NotImplementedException();
    }
}

public static class RuleHelpers
{
    public static ImmutableList<GridCoord> ReadGridCoords(JsonNode node, string key)
    {
        var prop = node[key];
        if (prop is null)
            throw new InvalidDataException($"Expected key {key} is not present");
        if (prop is not JsonArray arr)
            throw new InvalidDataException($"Expected key {key} is not an array");
        return ReadGridCoords(arr);
    }
    
    public static ImmutableList<GridCoord> ReadGridCoords(JsonArray array)
    {
        var b = ImmutableList.CreateBuilder<GridCoord>();
        for (int i = 0; i < array.Count; i++)
        {
            switch (array[i])
            {
                case JsonArray a:
                    b.Add(ValuesOrThrow<ushort, ushort>(a));
                    break;
                case var row:
                    var col = array[i++].GetValue<ushort>();
                    b.Add(new GridCoord(row.GetValue<ushort>(), col));
                    break;
            }
        }

        return b.ToImmutable();
    }
    
    public static JsonArray WriteGridCoords(ImmutableList<GridCoord> coords)
    {
        JsonArray arr = new JsonArray();
        foreach (var c in coords)
        {
            arr.Add(c.Row);
            arr.Add(c.Col);
        }

        return arr;
    }

    public static T ValueOrThrow<T>(JsonNode node, string key)
    {
        var prop = node[key];
        if (prop is null)
            throw new InvalidDataException($"Expected key {key} is not present");
        return prop.GetValue<T>();
    }

    public static (T1 a, T2 b) ValuesOrThrow<T1, T2>(JsonArray node)
    {
        if (node.Count != 2)
            throw new InvalidDataException($"Array should be 2 long, but was {node.Count}");
        return (node[0].GetValue<T1>(), node[1].GetValue<T2>());
    }
}