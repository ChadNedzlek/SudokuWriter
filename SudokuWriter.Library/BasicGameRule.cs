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
        Span<ushort> byRow = stackalloc ushort[nRows];
        Span<ushort> byColumn = stackalloc ushort[nColumns];
        Span<ushort> byBox = stackalloc ushort[nRows];
        int boxesPerRow = state.Cells.Columns / state.BoxColumns;
        for (int r = 0; r < nRows; r++)
        {
            ref ushort row = ref byRow[r];
            int br = (r / state.BoxRows) * boxesPerRow;
            for (int c = 0; c < nColumns; c++)
            {
                int v = state.Cells.GetSingle(r, c);
                if (v == -1) continue;
                ushort m = unchecked((ushort)(1 << v));

                if ((row & m) != 0) return GameResult.Unsolvable;
                row |= m;

                ref ushort col = ref byColumn[c];
                if ((col & m) != 0) return GameResult.Unsolvable;
                col |= m;

                int b = br + c / state.BoxColumns;
                ref ushort box = ref byBox[b];
                if ((box & m) != 0) return GameResult.Unsolvable;
                box |= m;
            }
        }

        ushort dMask = unchecked((ushort)((1 << state.Digits) - 1));
        if (byRow.ContainsAnyExcept(dMask) || byColumn.ContainsAnyExcept(dMask) || byBox.ContainsAnyExcept(dMask)) return GameResult.Unknown;

        return GameResult.Solved;
    }

    public bool TryReduce(ref GameState state)
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
                if (v == -1) continue;
                ushort m = unchecked((ushort)(1 << v));
                row |= m;
                byColumn[c] |= m;
                int b = br + c / state.Cells.Columns;
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
                int b = br + c / state.Cells.Columns;
                ushort m = unchecked((ushort)~(row | byColumn[c] | byBox[b]));
                ushort before = cell;
                cell &= m;
                if (cell != before)
                {
                    removed = true;
                }
            }
        }

        if (removed)
        {
            state = state.WithCells(cellBuilder.MoveToImmutable());
            return true;
        }

        return false;
    }
}