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

    public GameState? TryReduce(GameState state, ISimplificationChain chain)
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
                CellValueMask prevValue = cell;
                if (RuleHelpers.TryMask(ref cell, m))
                {
                    chain.Record($"Cell {r + 1},{c + 1} was {prevValue} - (row ~{row}, col ~{byColumn[c]}, ~{byBox[b]}) => {cell} ");
                    removed = true;
                }
            }
        }

        if (removed) return state.WithCells(cellBuilder.MoveToImmutable());

        return null;
    }

    public IEnumerable<MutexGroup> GetMutualExclusionGroups(GameState state, ISimplificationTracker tracker)
    {
        for (int r = 0; r < state.Structure.Rows; r++)
        {
            yield return new MutexGroup(state.Cells.GetRow(r).Box(), tracker.Record($"Mutex row {r+1}"));
        }
        
        for (int c = 0; c < state.Structure.Columns; c++)
        {
            yield return new MutexGroup(state.Cells.GetColumn(c).Box(), tracker.Record($"Mutex column {c+1}"));
        }

        for (int r = 0; r < state.Structure.Rows; r += state.BoxRows)
        {
            for (int c = 0; c < state.Structure.Columns; c+=state.BoxColumns)
            {
                yield return new MutexGroup(state.Cells.GetRange(r..(r + state.BoxRows), c..(c + state.BoxColumns)).Box(), tracker.Record($"Mutex box {r+1},{c+1}"));
            }
        }
    }

    public IEnumerable<DigitFence> GetFencedDigits(GameState state, ISimplificationTracker tracker)
    {
        List<DigitFence> fences = [];
        ReadOnlyMultiRef<CellValueMask> cells = state.Cells.GetEmptyReferences();
        CellValue maxDigit = new(state.Digits);
        for (CellValue digit = new(0); digit < maxDigit; digit++)
        {
            for (int r = 0; r < state.Structure.Rows; r++)
            {
                cells.Clear();
                int count = 0;
                for (int c = 0; c < state.Structure.Columns; c++)
                {
                    ref readonly CellValueMask value = ref state.Cells[r, c];
                    if (!value.Contains(digit)) continue;
                    cells.Include(in value);
                    count++;
                }

                if (count >= state.Digits) continue;
                fences.Add(new DigitFence(digit, cells.Box(), tracker.Record($"Fence row {r+1} ({digit} in {count} cells)")));
            }

            for (int c = 0; c < state.Structure.Columns; c++)
            {
                cells.Clear();
                int count = 0;
                for (int r = 0; r < state.Structure.Rows; r++)
                {
                    ref readonly CellValueMask value = ref state.Cells[r, c];
                    if (!value.Contains(digit)) continue;
                    cells.Include(in value);
                    count++;
                }

                if (count >= state.Digits) continue;
                fences.Add(new DigitFence(digit, cells.Box(), tracker.Record($"fence column {c+1} ({digit} in {count} cells)")));
            }
            
            for (int r = 0; r < state.Structure.Rows; r+=state.Structure.BoxRows)
            for (int c = 0; c < state.Structure.Columns; c += state.Structure.BoxColumns)
            {
                cells.Clear();
                int count = 0;
                for(int br = 0;br<state.Structure.BoxRows;br++)
                for (int bc = 0; bc < state.Structure.BoxColumns; bc++)
                {
                    ref readonly CellValueMask value = ref state.Cells[r + br, c + bc];
                    if (!value.Contains(digit)) continue;
                    cells.Include(in value);
                    count++;
                }
                if (count >= state.Digits) continue;
                fences.Add(new DigitFence(digit, cells.Box(), tracker.Record($"Box {r+1},{c+1} ({digit} in {count} cells)")));
            }
        }

        return fences;
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