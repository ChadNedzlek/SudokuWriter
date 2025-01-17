using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace SudokuWriter.Library.Rules;

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
            ushort a = state.Cells[aCoord];
            ushort b = state.Cells[bCoord];
            if ((a & GetDoubleOrHalves(b)) == 0) return GameResult.Unsolvable;
        }

        foreach (GridEdge s in Sequential)
        {
            var (aCoord, bCoord) = s.GetSides();
            ushort a = state.Cells[aCoord];
            ushort b = state.Cells[bCoord];
            if ((a & GetSequential(b)) == 0) return GameResult.Unsolvable;
        }

        return GameResult.Solved;
    }

    private static ushort GetDoubleOrHalves(ushort mask) => (ushort)(GetDoubles(mask) | GetHalves(mask));

    private static ushort GetDoubles(ushort mask)
    {
        return (ushort)(((mask & Cells.GetDigitMask(1)) << 1) |
            (ushort)((mask & Cells.GetDigitMask(2)) << 2) |
            (ushort)((mask & Cells.GetDigitMask(3)) << 3) |
            (ushort)((mask & Cells.GetDigitMask(4)) << 4));
    }

    private static ushort GetHalves(ushort mask)
    {
        return (ushort)(((mask & Cells.GetDigitMask(2)) >> 1) |
            (ushort)((mask & Cells.GetDigitMask(4)) >> 2) |
            (ushort)((mask & Cells.GetDigitMask(6)) >> 3) |
            (ushort)((mask & Cells.GetDigitMask(8)) >> 4));
    }

    private static ushort GetSequential(ushort mask)
    {
        return (ushort)((mask << 1) | (mask >> 1));
    }

    public GameState? TryReduce(GameState state)
    {
        CellsBuilder cells = state.Cells.ToBuilder();
        bool reduced = false;
        foreach (GridEdge d in Doubles)
        {
            var (aCoord, bCoord) = d.GetSides();
            ref ushort a = ref cells[aCoord];
            ref ushort b = ref cells[bCoord];
            reduced |= RuleHelpers.TryMask(ref a, GetDoubleOrHalves(b));
            reduced |= RuleHelpers.TryMask(ref b, GetDoubleOrHalves(a));
        }

        foreach (GridEdge s in Sequential)
        {
            var (aCoord, bCoord) = s.GetSides();
            ref ushort a = ref cells[aCoord];
            ref ushort b = ref cells[bCoord];
            reduced |= RuleHelpers.TryMask(ref a, GetSequential(b));
            reduced |= RuleHelpers.TryMask(ref b, GetSequential(a));
        }

        return reduced ? state.WithCells(cells.MoveToImmutable()) : null;
    }

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