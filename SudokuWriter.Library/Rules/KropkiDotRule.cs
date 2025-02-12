using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Nodes;

namespace VaettirNet.SudokuWriter.Library.Rules;

[GameRule("kropki")]
public class KropkiDotRule : IGameRule
{
    public KropkiDotRule(ImmutableArray<GridEdge> doubles, ImmutableArray<GridEdge> sequential)
    {
        Doubles = doubles;
        Sequential = sequential;
    }

    public ImmutableArray<GridEdge> Doubles { get; }
    public ImmutableArray<GridEdge> Sequential { get; }

    public GameResult Evaluate(GameState state)
    {
        foreach (GridEdge d in Doubles)
        {
            var (aCoord, bCoord) = d.GetSides();
            CellValueMask a = state.Cells[aCoord];
            CellValueMask b = state.Cells[bCoord];
            if ((a & GetDoubleOrHalves(b)) == CellValueMask.None) return GameResult.Unsolvable;
        }

        foreach (GridEdge s in Sequential)
        {
            var (aCoord, bCoord) = s.GetSides();
            CellValueMask a = state.Cells[aCoord];
            CellValueMask b = state.Cells[bCoord];
            if ((a & GetSequential(b)) == CellValueMask.None) return GameResult.Unsolvable;
        }

        return GameResult.Solved;
    }

    private static CellValueMask GetDoubleOrHalves(CellValueMask mask) => GetDoubles(mask) | GetHalves(mask);

    private static CellValueMask GetDoubles(CellValueMask mask)
    {
        return ((mask & new CellValue(0)) << 1) |
            ((mask & new CellValue(1)) << 2) |
            ((mask & new CellValue(2)) << 3) |
            ((mask & new CellValue(3)) << 4);
    }

    private static CellValueMask GetHalves(CellValueMask mask)
    {
        return ((mask & new CellValue(1)) >> 1) |
            ((mask & new CellValue(3)) >> 2) |
            ((mask & new CellValue(5)) >> 3) |
            ((mask & new CellValue(7)) >> 4);
    }

    private static CellValueMask GetSequential(CellValueMask mask)
    {
        return (mask << 1) | (mask >> 1);
    }

    public GameState? TryReduce(GameState state, ISimplificationChain chain)
    {
        CellsBuilder cells = state.Cells.ToBuilder();
        bool reduced = false;
        foreach (GridEdge d in Doubles)
        {
            var (aCoord, bCoord) = d.GetSides();
            ref CellValueMask a = ref cells[aCoord];
            ref CellValueMask b = ref cells[bCoord];
            reduced |= RuleHelpers.TryMask(ref a, GetDoubleOrHalves(b));
            reduced |= RuleHelpers.TryMask(ref b, GetDoubleOrHalves(a));
        }

        foreach (GridEdge s in Sequential)
        {
            var (aCoord, bCoord) = s.GetSides();
            ref CellValueMask a = ref cells[aCoord];
            ref CellValueMask b = ref cells[bCoord];
            reduced |= RuleHelpers.TryMask(ref a, GetSequential(b));
            reduced |= RuleHelpers.TryMask(ref b, GetSequential(a));
        }

        return reduced ? state.WithCells(cells.MoveToImmutable()) : null;
    }

    public IEnumerable<MutexGroup> GetMutualExclusionGroups(GameState state, ISimplificationTracker tracker) => [];
    public IEnumerable<DigitFence> GetFencedDigits(GameState state, ISimplificationTracker tracker) => [];

    public JsonObject ToJsonObject()
    {
        JsonArray doubles = new();
        foreach (var d in Doubles)
        {
            doubles.Add(new JsonArray(d.EdgeRow, d.EdgeCol));
        }
        JsonArray sequential = new();
        foreach (var s in Sequential)
        {
            sequential.Add(new JsonArray(s.EdgeRow, s.EdgeCol));
        }

        return new JsonObject
        {
            ["double"] = doubles,
            ["sequential"] = sequential,
        };
    }

    public static IGameRule FromJsonObject(JsonObject jsonObject)
    {
        ImmutableArray<GridEdge> doubleList = ImmutableArray<GridEdge>.Empty;
        if (jsonObject["double"] is JsonArray doubles)
        {
            doubleList = doubles.Select(a => new GridEdge(a[0]?.GetValue<ushort>() ?? 0, a[1]?.GetValue<ushort>() ?? 0)).ToImmutableArray();
        }
        
        ImmutableArray<GridEdge> sequentialList = ImmutableArray<GridEdge>.Empty;
        if (jsonObject["sequential"] is JsonArray sequential)
        {
            sequentialList = sequential.Select(a => new GridEdge(a[0]?.GetValue<ushort>() ?? 0, a[1]?.GetValue<ushort>() ?? 0)).ToImmutableArray();
        }

        return new KropkiDotRule(doubleList, sequentialList);
    }
}