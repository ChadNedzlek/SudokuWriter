using System.Collections.Immutable;
using System.IO;
using System.Text.Json.Nodes;

namespace SudokuWriter.Library.Rules;

public abstract class LineRule<T> : IGameRule where T:ILineRule<T>
{
    public readonly ImmutableList<LineRuleSegment> Segments;

    protected LineRule(ImmutableList<LineRuleSegment> segments)
    {
        Segments = segments;
    }

    public abstract GameResult Evaluate(GameState state);
    public abstract GameState? TryReduce(GameState state);

    public JsonObject ToJsonObject()
    {
        JsonObject o = new();
        JsonArray arr = new();
        foreach (LineRuleSegment part in Segments)
        {
            arr.Add(new JsonArray(part.Start.Row, part.Start.Col, part.End.Row, part.End.Col));
        }

        o["segments"] = arr;
        return o;
    }

    public static IGameRule FromJsonObject(JsonObject jsonObject)
    {
        if (jsonObject["segments"] is not JsonArray arr)
        {
            throw new InvalidDataException("Missing 'segments'");
        }

        ImmutableList<LineRuleSegment>.Builder b = ImmutableList.CreateBuilder<LineRuleSegment>();
        foreach (JsonNode o in arr)
        {
            if (o is not JsonArray { Count: 4 } part)
            {
                throw new InvalidDataException("Invalid 'segments'");
            }

            b.Add(
                new LineRuleSegment(
                    new GridCoord(part[0].GetValue<ushort>(), part[1].GetValue<ushort>()),
                    new GridCoord(part[2].GetValue<ushort>(), part[3].GetValue<ushort>())
                )
            );
        }

        return T.Create(b.ToImmutable(), jsonObject);
    }
}