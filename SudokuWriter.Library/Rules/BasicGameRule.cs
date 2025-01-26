using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Nodes;

namespace SudokuWriter.Library.Rules;

[GameRule("basic")]
public class BasicGameRule : IGameRule
{
    public static BasicGameRule Instance { get; } = new();

    public GameResult Evaluate(GameState state)
    {
        int nRows = state.Cells.Rows;
        int nColumns = state.Cells.Columns;
        Span<ushort> forcedByRow = stackalloc ushort[nRows];
        Span<ushort> forcedByColumn = stackalloc ushort[nColumns];
        Span<ushort> forcedByBox = stackalloc ushort[nRows];
        Span<ushort> allowedInRow = stackalloc ushort[nRows];
        Span<ushort> allowedInColumns = stackalloc ushort[nColumns];
        Span<ushort> allowedInBox = stackalloc ushort[nRows];
        int boxesPerRow = state.Cells.Columns / state.BoxColumns;
        for (int r = 0; r < nRows; r++)
        {
            ref ushort forcedRow = ref forcedByRow[r];
            ref ushort allowedRow = ref allowedInRow[r];
            int br = r / state.BoxRows * boxesPerRow;
            for (int c = 0; c < nColumns; c++)
            {
                int b = br + c / state.BoxColumns;
                ushort allowed = state.Cells[r, c];
                allowedRow |= allowed;
                allowedInColumns[c] |= allowed;
                allowedInBox[b] |= allowed;

                int v = state.Cells.GetSingle(r, c);
                if (v == Cells.NoSingleValue) continue;

                ushort m = unchecked((ushort)(1 << v));

                if ((forcedRow & m) != 0) return GameResult.Unsolvable;

                forcedRow |= m;

                ref ushort col = ref forcedByColumn[c];
                if ((col & m) != 0) return GameResult.Unsolvable;

                col |= m;
                ref ushort box = ref forcedByBox[b];
                if ((box & m) != 0) return GameResult.Unsolvable;

                box |= m;
            }
        }

        ushort dMask = unchecked((ushort)((1 << state.Digits) - 1));

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
        Span<ushort> byRow = stackalloc ushort[nRows];
        Span<ushort> byColumn = stackalloc ushort[nColumns];
        Span<ushort> byBox = stackalloc ushort[nRows];
        int boxesPerRow = state.Cells.Columns / state.BoxColumns;
        for (int r = 0; r < nRows; r++)
        {
            ref ushort row = ref byRow[r];
            int br = r / state.BoxRows * boxesPerRow;
            for (int c = 0; c < nColumns; c++)
            {
                int v = state.Cells.GetSingle(r, c);
                if (v == Cells.NoSingleValue) continue;

                ushort m = unchecked((ushort)(1 << v));
                row |= m;
                byColumn[c] |= m;
                int b = br + c / state.BoxColumns;
                byBox[b] |= m;
            }
        }

        bool removed = false;
        for (int r = 0; r < nRows; r++)
        {
            ushort row = byRow[r];
            int br = r / state.BoxRows * boxesPerRow;
            for (int c = 0; c < nColumns; c++)
            {
                ref ushort cell = ref cellBuilder[r, c];
                if (BitOperations.IsPow2(cell)) continue;

                int b = br + c / state.BoxColumns;
                ushort m = unchecked((ushort)~(row | byColumn[c] | byBox[b]));
                removed |= RuleHelpers.TryUpdate(ref cell, (ushort)(cell & m));
            }
        }

        if (removed) return state.WithCells(cellBuilder.MoveToImmutable());

        return null;
    }

    public IEnumerable<MultiRefBox<ushort>> GetMutualExclusionGroups(GameState state)
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