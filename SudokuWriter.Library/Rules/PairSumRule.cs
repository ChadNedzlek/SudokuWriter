using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;

namespace SudokuWriter.Library.Rules;

[GameRule("pair-sum")]
public class PairSumRule : IGameRule
{
    public ushort Sum { get; }
    public ImmutableArray<GridEdge> Pairs { get; }

    public PairSumRule(ushort sum, ImmutableArray<GridEdge> pairs)
    {
        Sum = sum;
        Pairs = pairs;
    }

    public GameResult Evaluate(GameState state)
    {
        foreach (GridEdge d in Pairs)
        {
            var (aCoord, bCoord) = d.GetSides();
            ushort a = state.Cells[aCoord];
            ushort b = state.Cells[bCoord];
            if ((a & GetSumPair(b, state.Digits)) == 0) return GameResult.Unsolvable;
        }

        return GameResult.Solved;
    }

    private ushort GetSumPair(ushort x, ushort digits)
    {
        int reversed = Cells.GetReversed(x, digits);
        int zeroShift = digits + 1;
        int shiftAmount = zeroShift - Sum;
        if (shiftAmount > 0)
        {
            return (ushort)(reversed >> shiftAmount);
        }

        return (ushort)(reversed << -shiftAmount);
    }

    public GameState? TryReduce(GameState state)
    {
        var cells = state.Cells.ToBuilder();
        bool reduced = false;
        foreach (GridEdge d in Pairs)
        {
            var (aCoord, bCoord) = d.GetSides();
            ref ushort a = ref cells[aCoord];
            ref ushort b = ref cells[bCoord];
            reduced |= RuleHelpers.TryMask(ref a, GetSumPair(b, state.Digits));
            reduced |= RuleHelpers.TryMask(ref b, GetSumPair(a, state.Digits));
        }

        return reduced ? state.WithCells(cells.MoveToImmutable()) : null;
    }

    public JsonObject ToJsonObject()
    {
        JsonArray pairs = new();
        foreach (var d in Pairs)
        {
            pairs.Add(new JsonArray(d.EdgeRow, d.EdgeCol));
        }
        return new JsonObject
        {
            ["pairs"] = pairs,
            ["sum"] = Sum,
        };
    }

    public static IGameRule FromJsonObject(JsonObject jsonObject)
    {
        ImmutableArray<GridEdge> pairList = ImmutableArray<GridEdge>.Empty;
        if (jsonObject["pairs"] is JsonArray pairs)
        {
            pairList = pairs.Select(a => new GridEdge(a[0]?.GetValue<ushort>() ?? 0, a[1]?.GetValue<ushort>() ?? 0)).ToImmutableArray();
        }

        ushort sum = RuleHelpers.ValueOrThrow<ushort>(jsonObject, "sum");

        return new PairSumRule(sum, pairList);
    }
}