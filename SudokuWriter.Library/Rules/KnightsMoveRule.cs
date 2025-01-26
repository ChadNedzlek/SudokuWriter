using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace VaettirNet.SudokuWriter.Library.Rules;

[GameRule("knight")]
public class KnightsMoveRule : IGameRule
{
    private static readonly ImmutableArray<GridOffset> s_knightOffsets =
    [
        // We only need half the circle, because every pair goes both ways
        (2, 1), (1, 2), (-1, 2), (-2, 1)
    ];  
    
    public GameResult Evaluate(GameState state)
    {
        for (int r = 0; r < state.Structure.Rows; r++)
        for (int c = 0; c < state.Structure.Columns; c++)
        {
            int single = state.Cells.GetSingle(r, c);
            if (single == Cells.NoSingleValue)
            {
                continue;
            }

            foreach (GridOffset ko in s_knightOffsets)
            {
                int ro = r + ko.Row;
                int co = c + ko.Col;
                
                if (ro < 0 || ro >= state.Structure.Rows) continue;
                if (co < 0 || co >= state.Structure.Columns) continue;

                int knightSingle = state.Cells.GetSingle(ro, co);
                if (knightSingle == single)
                {
                    return GameResult.Unsolvable;
                }
            }
        }

        return GameResult.Solved;
    }

    public GameState? TryReduce(GameState state)
    {
        bool changed = false;
        CellsBuilder cells = state.Cells.ToBuilder();
        for (ushort r = 0; r < state.Structure.Rows; r++)
        for (ushort c = 0; c < state.Structure.Columns; c++)
        {
            var value = cells[r, c];
            int single = Cells.GetSingle(value);
            if (single == Cells.NoSingleValue) continue;

            MultiRef<ushort> seenCells = cells.GetEmptyReferences();
            foreach (GridOffset offset in s_knightOffsets)
            {
                if (cells.IsInRange(r, c, offset))
                {
                    seenCells.Include(ref cells[(r,c) + offset]);
                }

                if (cells.IsInRange(r, c, -offset))
                {
                    seenCells.Include(ref cells[(r,c) - offset]);
                }
            }

            changed |= RuleHelpers.ClearFromSeenCells(value, seenCells);
        }

        return changed ? state.WithCells(cells.MoveToImmutable()) : null;
    }

    public IEnumerable<MultiRefBox<ushort>> GetMutualExclusionGroups(GameState state) => [];

    public JsonObject ToJsonObject() => new();

    public static IGameRule FromJsonObject(JsonObject jsonObject) => new KnightsMoveRule();
}