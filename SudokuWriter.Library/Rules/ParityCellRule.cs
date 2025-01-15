using System;
using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace SudokuWriter.Library.Rules;

[GameRule("pcell")]
public class ParityCellRule : IGameRule
{
    public ParityCellRule(ImmutableList<GridCoord> evenCells, ImmutableList<GridCoord> oddCells)
    {
        EvenCells = evenCells;
        OddCells = oddCells;
    }

    public ImmutableList<GridCoord> EvenCells { get; }
    public ImmutableList<GridCoord> OddCells { get; }

    public GameResult Evaluate(GameState state)
    {
        foreach (var odd in OddCells)
        {
            var single = state.Cells.GetSingle(odd.Row, odd.Col);
            if (single == -1) continue;
            // Digits are 0 based in code, 1 based in rules
            if (single % 2 != 0) return GameResult.Unsolvable;
        }

        foreach (var even in EvenCells)
        {
            var single = state.Cells.GetSingle(even.Row, even.Col);
            if (single == -1) continue;
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
        foreach (var odd in OddCells)
        {
            ref ushort mask = ref cells[odd.Row, odd.Col];
            ushort newMask = unchecked((ushort)(oddMask & mask));
            if (newMask != mask) changed = true;
            mask = newMask;
        }

        foreach (var even in EvenCells)
        {
            ref ushort mask = ref cells[even.Row, even.Col];
            ushort newMask = unchecked((ushort)((oddMask << 1) & mask));
            if (newMask != mask) changed = true;
            mask = newMask;
        }

        return changed ? state.WithCells(cells.MoveToImmutable()) : null;
    }

    public JsonObject ToJsonObject()
    {
        return new JsonObject
        {
            ["odd"] = RuleHelpers.WriteGridCoords(OddCells),
            ["event"] = RuleHelpers.WriteGridCoords(EvenCells),
        };
    }

    public static IGameRule FromJsonObject(JsonObject jsonObject)
    {
        var odd = RuleHelpers.ReadGridCoords(jsonObject, "odd");
        var even = RuleHelpers.ReadGridCoords(jsonObject, "even");
        return new ParityCellRule(even, odd);
    }
}