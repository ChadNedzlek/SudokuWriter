using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace VaettirNet.SudokuWriter.Library.Rules;

[GameRule("basic")]
public class BasicGameRule : IGameRule
{
    public static BasicGameRule Instance { get; } = new();

    public GameResult Evaluate(GameState state)
    {
        int nRows = state.Cells.Rows;
        int nColumns = state.Cells.Columns;
        Span<CellValueMask> forcedByRow = stackalloc CellValueMask[nRows];
        Span<CellValueMask> forcedByColumn = stackalloc CellValueMask[nColumns];
        Span<CellValueMask> forcedByBox = stackalloc CellValueMask[nRows];
        Span<CellValueMask> allowedInRow = stackalloc CellValueMask[nRows];
        Span<CellValueMask> allowedInColumns = stackalloc CellValueMask[nColumns];
        Span<CellValueMask> allowedInBox = stackalloc CellValueMask[nRows];
        int boxesPerRow = state.Cells.Columns / state.BoxColumns;
        for (int r = 0; r < nRows; r++)
        {
            ref CellValueMask forcedRow = ref forcedByRow[r];
            ref CellValueMask allowedRow = ref allowedInRow[r];
            int br = r / state.BoxRows * boxesPerRow;
            for (int c = 0; c < nColumns; c++)
            {
                int b = br + c / state.BoxColumns;
                CellValueMask allowed = state.Cells[r, c];
                allowedRow |= allowed;
                allowedInColumns[c] |= allowed;
                allowedInBox[b] |= allowed;

                if (!state.Cells[r, c].TryGetSingle(out var v)) continue;

                CellValueMask m = v.AsMask();

                if ((forcedRow & m) != CellValueMask.None) return GameResult.Unsolvable;

                forcedRow |= m;

                ref CellValueMask col = ref forcedByColumn[c];
                if ((col & m) != CellValueMask.None) return GameResult.Unsolvable;

                col |= m;
                ref CellValueMask box = ref forcedByBox[b];
                if ((box & m) != CellValueMask.None) return GameResult.Unsolvable;

                box |= m;
            }
        }

        CellValueMask dMask = CellValueMask.All(state.Digits);

        if (allowedInRow.ContainsAnyExcept(dMask) ||
            allowedInColumns.ContainsAnyExcept(dMask) ||
            allowedInBox.ContainsAnyExcept(dMask))
            return GameResult.Unsolvable;

        if (forcedByRow.ContainsAnyExcept(dMask) ||
            forcedByColumn.ContainsAnyExcept(dMask) ||
            forcedByBox.ContainsAnyExcept(dMask))
            return GameResult.Unknown;

        return GameResult.Solved;
    }

    public GameState? TryReduce(GameState state)
    {
        CellsBuilder cellBuilder = state.Cells.ToBuilder();
        int nRows = state.Cells.Rows;
        int nColumns = state.Cells.Columns;
        Span<CellValueMask> byRow = stackalloc CellValueMask[nRows];
        Span<CellValueMask> byColumn = stackalloc CellValueMask[nColumns];
        Span<CellValueMask> byBox = stackalloc CellValueMask[nRows];
        int boxesPerRow = state.Cells.Columns / state.BoxColumns;
        for (int r = 0; r < nRows; r++)
        {
            ref CellValueMask row = ref byRow[r];
            int br = r / state.BoxRows * boxesPerRow;
            for (int c = 0; c < nColumns; c++)
            {
                if (!state.Cells[r, c].TryGetSingle(out var v)) continue;

                CellValueMask m = v.AsMask();
                row |= m;
                byColumn[c] |= m;
                int b = br + c / state.BoxColumns;
                byBox[b] |= m;
            }
        }

        bool removed = false;
        for (int r = 0; r < nRows; r++)
        {
            CellValueMask row = byRow[r];
            int br = r / state.BoxRows * boxesPerRow;
            for (int c = 0; c < nColumns; c++)
            {
                ref CellValueMask cell = ref cellBuilder[r, c];
                if (cell.IsSingle()) continue;

                int b = br + c / state.BoxColumns;
                CellValueMask m = ~(row | byColumn[c] | byBox[b]);
                removed |= RuleHelpers.TryUpdate(ref cell, cell & m);
            }
        }

        if (removed) return state.WithCells(cellBuilder.MoveToImmutable());

        return null;
    }

    public IEnumerable<MultiRefBox<CellValueMask>> GetMutualExclusionGroups(GameState state)
    {
        for (int r = 0; r < state.Structure.Rows; r++)
        {
            yield return state.Cells.GetRow(r).Box();
        }
        for (int c = 0; c < state.Structure.Columns; c++)
        {
            yield return state.Cells.GetColumn(c).Box();
        }

        for (int r = 0; r < state.Structure.Rows; r += state.BoxRows)
        {
            for (int c = 0; c < state.Structure.Columns; c+=state.BoxColumns)
            {
                yield return state.Cells.GetRange(r..(r + state.BoxRows), c..(c + state.BoxColumns)).Box();
            }
        }
    }

    public JsonObject ToJsonObject()
    {
        return new JsonObject();
    }

    public static IGameRule FromJsonObject(JsonObject jsonObject)
    {
        return Instance;
    }
}