using System;
using System.Numerics;

namespace SudokuWriter.Library;

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
        for (var r = 0; r < nRows; r++)
        {
            ref ushort forcedRow = ref forcedByRow[r];
            ref ushort allowedRow = ref allowedInRow[r];
            int br = r / state.BoxRows * boxesPerRow;
            for (var c = 0; c < nColumns; c++)
            {
                int b = br + c / state.BoxColumns;
                ushort allowed = state.Cells.GetMask(r, c);
                allowedRow |= allowed;
                allowedInColumns[c] |= allowed;
                allowedInBox[b] |= allowed;

                int v = state.Cells.GetSingle(r, c);
                if (v == -1) continue;
                var m = unchecked((ushort)(1 << v));

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

        var dMask = unchecked((ushort)((1 << state.Digits) - 1));

        if (allowedInRow.ContainsAnyExcept(dMask) || allowedInColumns.ContainsAnyExcept(dMask) || allowedInBox.ContainsAnyExcept(dMask))
            return GameResult.Unsolvable;
        if (forcedByRow.ContainsAnyExcept(dMask) || forcedByColumn.ContainsAnyExcept(dMask) || forcedByBox.ContainsAnyExcept(dMask))
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
        for (var r = 0; r < nRows; r++)
        {
            ref ushort row = ref byRow[r];
            int br = r / state.BoxRows * boxesPerRow;
            for (var c = 0; c < nColumns; c++)
            {
                int v = state.Cells.GetSingle(r, c);
                if (v == -1) continue;
                var m = unchecked((ushort)(1 << v));
                row |= m;
                byColumn[c] |= m;
                int b = br + c / state.BoxColumns;
                byBox[b] |= m;
            }
        }

        var removed = false;
        for (var r = 0; r < nRows; r++)
        {
            ushort row = byRow[r];
            int br = r / state.BoxRows * boxesPerRow;
            for (var c = 0; c < nColumns; c++)
            {
                ref ushort cell = ref cellBuilder[r, c];
                if (BitOperations.IsPow2(cell)) continue;
                int b = br + c / state.BoxColumns;
                var m = unchecked((ushort)~(row | byColumn[c] | byBox[b]));
                ushort before = cell;
                cell &= m;
                if (cell != before) removed = true;
            }
        }

        if (removed) return state.WithCells(cellBuilder.MoveToImmutable());

        return null;
    }
}