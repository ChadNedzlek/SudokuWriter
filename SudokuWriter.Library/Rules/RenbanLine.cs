using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;

namespace VaettirNet.SudokuWriter.Library.Rules;

[GameRule("renban")]
public class RenbanLine : LineRule<RenbanLine>, ILineRule<RenbanLine>
{
    public RenbanLine(params ImmutableArray<BranchingRuleLine> lines) : base(lines)
    {
    }

    public RenbanLine(params ImmutableArray<LineRuleSegment> lines) : this(new BranchingRuleLine(lines))
    {
    }

    public RenbanLine(params ImmutableArray<GridCoord> cells) : this(new LineRuleSegment(cells))
    {
    }

    public static IGameRule Create(ImmutableArray<BranchingRuleLine> parts, JsonObject jsonObject)
    {
        return new RenbanLine(parts);
    }

    public override GameResult Evaluate(GameState state)
    {
        bool empty = false;
        foreach (BranchingRuleLine line in Lines)
        {
            CellValueMask setMask = CellValueMask.None;
            int length = 0;
            int setCount = 0;
            foreach (LineRuleSegment segment in line.Branches)
            {
                foreach (GridCoord coord in segment.Cells)
                {
                    length++;
                    CellValueMask mask = state.Cells[coord];
                    if (mask.IsSingle())
                    {
                        setCount++;
                        setMask |= mask;
                    }
                    else
                        empty = true;
                }
            }

            int range = sizeof(ushort) * 8 - BitOperations.LeadingZeroCount(setMask.RawValue) - BitOperations.TrailingZeroCount(setMask.RawValue);
            if (range > length) return GameResult.Unsolvable;

            ushort setBitCount = setMask.Count;
            if (setCount == length)
            {
                if (setBitCount != length)
                    return GameResult.Unsolvable;
            }
        }

        return empty ? GameResult.Unknown : GameResult.Solved;
    }

    public override GameState? TryReduce(GameState state, ISimplificationChain chain)
    {
        bool reduced = false;
        CellsBuilder cells = state.Cells.ToBuilder();

        CellValueMask allDigitMask = CellValueMask.All(state.Digits);

        foreach (BranchingRuleLine line in Lines)
        {
            ushort length = 0;
            CellValueMask alreadySetMask = CellValueMask.None;
            CellValueMask availableDigits = CellValueMask.None;
            foreach (LineRuleSegment segment in line.Branches)
            {
                foreach (GridCoord coord in segment.Cells)
                {
                    length++;
                    CellValueMask cell = cells[coord];
                    availableDigits |= cell;
                    if (cell.IsSingle()) alreadySetMask |= cell;
                }
            }

            // Theoretically I need to "turn off" any bits that have gaps,
            // if like non of the cells can have a "3", then a renban of length 4
            // can't have 1 or 2 on it either, since it can't... fit.
            ushort range = unchecked((ushort)((1 << length) - 1));

            ushort checkGaps = availableDigits.RawValue;
            CellValueMask noGaps = CellValueMask.None;
            while (checkGaps != 0)
            {
                ushort trailingZeroCount = ushort.TrailingZeroCount(checkGaps);
                int checkWithLowSets = checkGaps | (checkGaps - 1);
                ushort lowestSetBits = (ushort)((~0 << trailingZeroCount) & checkWithLowSets & ~(checkWithLowSets & (checkWithLowSets + 1)));
                ushort test = (ushort)(range << trailingZeroCount);
                if ((test & checkGaps) == test)
                {
                    // The range fits, blit it the contiguous set 1 bits
                    noGaps |= new CellValueMask(lowestSetBits);
                }

                // We checked the bottom pile of bits, clear them to see if there's another range that works
                checkGaps &= (ushort)~lowestSetBits;
            }

            CellValue maximumLowDigit = state.MinDigit;
            CellValue minimumHighDigit = state.MaxDigit;
            foreach (LineRuleSegment branch in line.Branches)
            {
                foreach (GridCoord coord in branch.Cells)
                {
                    ref CellValueMask cell = ref cells[coord];
                    reduced |= RuleHelpers.TryMask(ref cell, noGaps);
                    minimumHighDigit = CellValue.Min(minimumHighDigit, cell.GetMaxValue());
                    maximumLowDigit = CellValue.Max(maximumLowDigit, cell.GetMinValue());
                }
            }

            int highDigitsToStrip = state.Digits - length - minimumHighDigit.NumericValue + 1;
            CellValueMask stripTooHighBits = highDigitsToStrip < 1 ? allDigitMask : allDigitMask >> highDigitsToStrip;
            int lowDigitsToStrip = maximumLowDigit.NumericValue - length;
            CellValueMask stripTooLowBits = lowDigitsToStrip < 1 ? allDigitMask : allDigitMask << lowDigitsToStrip;
            CellValueMask allowedMask = stripTooHighBits & stripTooLowBits & ~alreadySetMask;

            foreach (LineRuleSegment branch in line.Branches)
            {
                foreach (GridCoord cell in branch.Cells)
                {
                    ref CellValueMask mask = ref cells[cell];
                    if (mask.IsSingle()) continue;

                    reduced |= RuleHelpers.TryMask(ref mask, allowedMask);
                }
            }
        }

        return reduced ? state.WithCells(cells.MoveToImmutable()) : null;
    }

    public override IEnumerable<MutexGroup> GetMutualExclusionGroups(GameState state, ISimplificationTracker tracker)
    {
        List<MutexGroup> groups = new();
        foreach (BranchingRuleLine line in Lines)
        {
            ReadOnlyMultiRef<CellValueMask> refs = state.Cells.GetEmptyReferences();
            foreach (LineRuleSegment branch in line.Branches)
            foreach (GridCoord cell in branch.Cells)
                refs.Include(in state.Cells[cell]);

            groups.Add(new(refs.Box(), tracker.Record($"Renban line at {line.Branches[0].Cells[0]}")));
        }

        return groups;
    }

    public override IEnumerable<DigitFence> GetFencedDigits(GameState state, ISimplificationTracker tracker)
    {
        List<DigitFence> fences = new();
        foreach (BranchingRuleLine line in Lines)
        {
            CellValue maximumLowDigit = state.MinDigit;
            CellValue minimumHighDigit = state.MaxDigit;
            ushort count = 0;
            foreach(GridCoord cell in line.Branches.SelectMany(b => b.Cells))
            {
                count++;
                CellValueMask cellMask = state.Cells[cell];
                minimumHighDigit = CellValue.Min(minimumHighDigit, cellMask.GetMaxValue());
                maximumLowDigit = CellValue.Max(maximumLowDigit, cellMask.GetMinValue());
            }

            CellValue minDigit = CellValue.Min(minimumHighDigit, state.MaxDigit - count + 1);
            CellValue maxDigit = CellValue.Max(maximumLowDigit, state.MinDigit + count - 1);

            for (var d = minDigit; d <= maxDigit; d++)
            {
                ReadOnlyMultiRef<CellValueMask> cells = state.Cells.GetEmptyReferences();
                foreach (GridCoord cell in line.Branches.SelectMany(b => b.Cells))
                {
                    ref readonly var mask = ref state.Cells[cell];
                    if (state.Cells[cell].Contains(d))
                        cells.Include(in mask);
                }

                if (cells.GetCount() != 1)
                    fences.Add(new(d, cells.Box(), tracker.Record($"Renban line at {line.Branches[0].Cells[0]} must contain {d}")));
            }
        }

        return fences;
    }
}