using System.Collections.Generic;
using System.Collections.Immutable;
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

    public override GameState? TryReduce(GameState state)
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

            ushort maximumLowDigit = 0;
            ushort minimumHighDigit = (ushort)(state.Digits - 1);
            foreach (LineRuleSegment branch in line.Branches)
            {
                foreach (GridCoord coord in branch.Cells)
                {
                    ref CellValueMask cell = ref cells[coord];
                    reduced |= RuleHelpers.TryMask(ref cell, noGaps);
                    minimumHighDigit = ushort.Min(minimumHighDigit, unchecked((ushort)(16 - ushort.LeadingZeroCount(cell.RawValue) - 1)));
                    maximumLowDigit = ushort.Max(maximumLowDigit, ushort.TrailingZeroCount(cell.RawValue));
                }
            }

            int highDigitsToStrip = state.Digits - length - minimumHighDigit;
            CellValueMask stripTooHighBits = highDigitsToStrip < 1 ? allDigitMask : allDigitMask >> highDigitsToStrip;
            int lowDigitsToStrip = maximumLowDigit - length + 1;
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

    public override IEnumerable<MultiRefBox<CellValueMask>> GetMutualExclusionGroups(GameState state)
    {
        foreach (BranchingRuleLine line in Lines)
        {
            ReadOnlyMultiRef<CellValueMask> refs = state.Cells.GetEmptyReferences();
            foreach (LineRuleSegment branch in line.Branches)
            foreach (GridCoord cell in branch.Cells)
                refs.Include(in state.Cells[cell]);

            yield return refs.Box();
        }
    }
}