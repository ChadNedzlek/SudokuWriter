using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Nodes;

namespace VaettirNet.SudokuWriter.Library.Rules;

[GameRule("pair-sum")]
public class PairSumRule : IGameRule
{
    public PairSumRule(ushort sum, ImmutableArray<GridEdge> pairs)
    {
        Sum = sum;
        Pairs = pairs;
    }

    public ushort Sum { get; }
    public ImmutableArray<GridEdge> Pairs { get; }

    public GameResult Evaluate(GameState state)
    {
        foreach (GridEdge d in Pairs)
        {
            (GridCoord aCoord, GridCoord bCoord) = d.GetSides();
            CellValueMask a = state.Cells[aCoord];
            CellValueMask b = state.Cells[bCoord];
            if ((a & GetSumPair(b, state.Digits)) == CellValueMask.None) return GameResult.Unsolvable;
        }

        return GameResult.Solved;
    }

    public GameState? TryReduce(GameState state)
    {
        CellsBuilder cells = state.Cells.ToBuilder();
        bool reduced = false;
        foreach (GridEdge d in Pairs)
        {
            (GridCoord aCoord, GridCoord bCoord) = d.GetSides();
            ref CellValueMask a = ref cells[aCoord];
            ref CellValueMask b = ref cells[bCoord];
            reduced |= RuleHelpers.TryMask(ref a, GetSumPair(b, state.Digits));
            reduced |= RuleHelpers.TryMask(ref b, GetSumPair(a, state.Digits));
        }

        return reduced ? state.WithCells(cells.MoveToImmutable()) : null;
    }

    public IEnumerable<MultiRefBox<CellValueMask>> GetMutualExclusionGroups(GameState state)
    {
        return [];
    }

    public JsonObject ToJsonObject()
    {
        JsonArray pairs = new();
        foreach (GridEdge d in Pairs) pairs.Add(new JsonArray(d.EdgeRow, d.EdgeCol));
        return new JsonObject { ["pairs"] = pairs, ["sum"] = Sum };
    }

    public static IGameRule FromJsonObject(JsonObject jsonObject)
    {
        var pairList = ImmutableArray<GridEdge>.Empty;
        if (jsonObject["pairs"] is JsonArray pairs)
            pairList = pairs.Select(a => new GridEdge(a[0]?.GetValue<ushort>() ?? 0, a[1]?.GetValue<ushort>() ?? 0)).ToImmutableArray();

        ushort sum = RuleHelpers.ValueOrThrow<ushort>(jsonObject, "sum");

        return new PairSumRule(sum, pairList);
    }

    private CellValueMask GetSumPair(CellValueMask x, ushort digits)
    {
        CellValueMask reversed = x.Reversed(digits);
        int zeroShift = digits + 1;
        int shiftAmount = zeroShift - Sum;
        if (shiftAmount > 0) return reversed >> (ushort)shiftAmount;

        return reversed << (ushort)-shiftAmount;
    }
}