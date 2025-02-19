using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace VaettirNet.SudokuWriter.Library.Rules;

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
            if (!state.Cells[odd].TryGetSingle(out var single)) continue;
            if (single.NumericValue % 2 == 0) return GameResult.Unsolvable;
        }

        foreach (GridCoord even in EvenCells)
        {
            if (!state.Cells[even].TryGetSingle(out var single)) continue;
            if (single.NumericValue % 2 != 0) return GameResult.Unsolvable;
        }

        return GameResult.Solved;
    }

    public GameState? TryReduce(GameState state, ISimplificationChain chain)
    {
        CellValueMask oddMask = new CellValueMask(0x5555) & CellValueMask.All(state.Digits);
        CellsBuilder cells = state.Cells.ToBuilder();
        bool changed = false;
        foreach (GridCoord odd in OddCells)
        {
            changed |= RuleHelpers.TryMask(ref cells[odd.Row, odd.Col], oddMask);
        }

        foreach (GridCoord even in EvenCells)
        {
            changed |= RuleHelpers.TryMask(ref cells[even.Row, even.Col], oddMask << 1);
        }

        return changed ? state.WithCells(cells.MoveToImmutable()) : null;
    }

    public IEnumerable<MutexGroup> GetMutualExclusionGroups(GameState state, ISimplificationTracker tracker) => [];
    public IEnumerable<DigitFence> GetFencedDigits(GameState state, ISimplificationTracker tracker) => [];

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