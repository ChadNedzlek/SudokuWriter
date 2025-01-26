using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace SudokuWriter.Library.Rules;

[GameRule("pcell")]
public class ParityCellRule : IGameRule
{
    public ParityCellRule(ImmutableArray<GridCoord> evenCells, ImmutableArray<GridCoord> oddCells)
    {
        EvenCells = evenCells;
        OddCells = oddCells;
    }

    public ImmutableArray<GridCoord> EvenCells { get; }
    public ImmutableArray<GridCoord> OddCells { get; }

    public GameResult Evaluate(GameState state)
    {
        foreach (GridCoord odd in OddCells)
        {
            int single = state.Cells.GetSingle(odd.Row, odd.Col);
            if (single == Cells.NoSingleValue) continue;
            // Digits are 0 based in code, 1 based in rules
            if (single % 2 != 0) return GameResult.Unsolvable;
        }

        foreach (GridCoord even in EvenCells)
        {
            int single = state.Cells.GetSingle(even.Row, even.Col);
            if (single == Cells.NoSingleValue) continue;
            // Digits are 0 based in code, 1 based in rules
            if (single % 2 == 0) return GameResult.Unsolvable;
        }

        return GameResult.Solved;
    }

    public GameState? TryReduce(GameState state)
    {
        ushort oddMask = unchecked((ushort)(0x5555 & Cells.GetAllDigitsMask(state.Digits)));
        CellsBuilder cells = state.Cells.ToBuilder();
        bool changed = false;
        foreach (GridCoord odd in OddCells)
        {
            changed |= RuleHelpers.TryMask(ref cells[odd.Row, odd.Col], oddMask);
        }

        foreach (GridCoord even in EvenCells)
        {
            changed |= RuleHelpers.TryMask(ref cells[even.Row, even.Col], unchecked((ushort)(oddMask << 1)));
        }

        return changed ? state.WithCells(cells.MoveToImmutable()) : null;
    }

    public IEnumerable<MultiRefBox<ushort>> GetMutualExclusionGroups(GameState state) => [];

    public JsonObject ToJsonObject()
    {
        return new JsonObject
        {
            ["odd"] = RuleHelpers.WriteGridCoords(OddCells),
            ["even"] = RuleHelpers.WriteGridCoords(EvenCells),
        };
    }

    public static IGameRule FromJsonObject(JsonObject jsonObject)
    {
        ImmutableArray<GridCoord> odd = RuleHelpers.ReadGridCoords(jsonObject, "odd");
        ImmutableArray<GridCoord> even = RuleHelpers.ReadGridCoords(jsonObject, "even");
        return new ParityCellRule(even, odd);
    }
}