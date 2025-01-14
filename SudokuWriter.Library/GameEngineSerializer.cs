using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace SudokuWriter.Library;

public class GameEngineSerializer
{
    public Task<GameEngine> LoadGameAsync(Stream source) => throw null;

    public async Task SaveGameAsync(GameEngine game, Stream destination)
    {
        await using Utf8JsonWriter writer = new(destination);
        writer.WriteStartObject();
        writer.WriteNumber("rows", game.InitialState.Structure.Rows);
        writer.WriteNumber("columns", game.InitialState.Structure.Columns);
        writer.WriteNumber("digits", game.InitialState.Structure.Digits);
        writer.WriteNumber("boxRows", game.InitialState.Structure.BoxRows);
        writer.WriteNumber("boxColumns", game.InitialState.Structure.BoxColumns);
        writer.WriteStartArray("cells");
        for (int r = 0; r < game.InitialState.Cells.Rows; r++)
        for (int c = 0; c < game.InitialState.Cells.Columns; c++)
        {
            int single = game.InitialState.Cells.GetSingle(r, c);
            if (single != -1)
            {
                writer.WriteNumberValue(r);
                writer.WriteNumberValue(c);
                writer.WriteNumberValue(single);
            }
        }
        writer.WriteEndArray();
        writer.WriteStartArray("rules");
        foreach (var rule in game.Rules)
        {
            var ruleName = rule.GetType().GetCustomAttribute<GameRuleAttribute>()?.Name;
            if (ruleName is null)
            {
                throw new ArgumentException($"Unknown game rule {rule.GetType().Name}");
            }

            writer.WriteStartArray();
            writer.WriteStringValue(ruleName);

            rule.WriteRule(writer);

            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }
}