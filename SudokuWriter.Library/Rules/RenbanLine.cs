using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace SudokuWriter.Library.Rules;

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
    
    public static IGameRule Create(ImmutableArray<BranchingRuleLine> parts, JsonObject jsonObject) => new RenbanLine(parts);
    
    public override GameResult Evaluate(GameState state)
    {
        bool empty = false;
        foreach (var line in Lines)
        {
            ushort setMask = 0;
            int length = 0;
            int setCount = 0;
            foreach (LineRuleSegment segment in line.Branches)
            {
                foreach (GridCoord coord in segment.Cells)
                {
                    length++;
                    ushort mask = state.Cells[coord];
                    if (Cells.IsSingle(mask))
                    {
                        setCount++;
                        setMask |= mask;
                    }
                    else
                    {
                        empty = true;
                    }
                }
            }

            int range = sizeof(ushort) * 8 - BitOperations.LeadingZeroCount(setMask) - BitOperations.TrailingZeroCount(setMask);
            if (range > length)
            {
                return GameResult.Unsolvable;
            }

            int setBitCount = BitOperations.PopCount(setMask);
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
        var cells = state.Cells.ToBuilder();

        ushort allDigitMask = Cells.GetAllDigitsMask(state.Digits);
        
        foreach (var line in Lines)
        {
            ushort length = 0;
            ushort alreadySetMask = 0;
            ushort availableDigits = 0;
            foreach (LineRuleSegment segment in line.Branches)
            {
                foreach (GridCoord coord in segment.Cells)
                {
                    length++;
                    ushort cell = cells[coord];
                    availableDigits |= cell;
                    if (Cells.IsSingle(cell)) alreadySetMask |= cell;
                }
            }
            
            // Theoretically I need to "turn off" any bits that have gaps,
            // if like non of the cells can have a "3", then a renban of length 4
            // can't have 1 or 2 on it either, since it can't... fit.
            ushort range = unchecked((ushort)((1 << length) - 1));

            ushort checkGaps = availableDigits;
            ushort noGaps = 0;
            while (checkGaps != 0)
            {
                ushort trailingZeroCount = ushort.TrailingZeroCount(checkGaps);
                int checkWithLowSets = checkGaps | (checkGaps - 1);
                ushort lowestSetBits = (ushort)((~0 << trailingZeroCount) & checkWithLowSets & ~(checkWithLowSets & (checkWithLowSets + 1)));
                ushort test = (ushort)(range << trailingZeroCount);
                if ((test & checkGaps) == test)
                {
                    // The range fits, blit it the contiguous set 1 bits
                    noGaps |= lowestSetBits;
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
                    ref ushort cell = ref cells[coord];
                    reduced |= RuleHelpers.TryMask(ref cell, noGaps);
                    minimumHighDigit = ushort.Min(minimumHighDigit, unchecked((ushort)(16 - ushort.LeadingZeroCount(cell) - 1)));
                    maximumLowDigit = ushort.Max(maximumLowDigit, ushort.TrailingZeroCount(cell));
                }
            }

            int highDigitsToStrip = state.Digits - length - minimumHighDigit;
            int stripTooHighBits = highDigitsToStrip < 1 ? allDigitMask : allDigitMask >> highDigitsToStrip;
            int lowDigitsToStrip = maximumLowDigit - length + 1;
            int stripTooLowBits = lowDigitsToStrip < 1 ? allDigitMask : allDigitMask << lowDigitsToStrip;
            ushort allowedMask = unchecked((ushort)(stripTooHighBits & stripTooLowBits & ~alreadySetMask));

            foreach (var branch in line.Branches)
            {
                foreach (GridCoord cell in branch.Cells)
                {
                    ref ushort mask = ref cells[cell];
                    if (Cells.IsSingle(mask)) continue;

                    reduced |= RuleHelpers.TryMask(ref mask, allowedMask);
                }
            }
        }

        return reduced ? state.WithCells(cells.MoveToImmutable()) : null;
    }

    public override IEnumerable<MultiRefBox<ushort>> GetMutualExclusionGroups(GameState state)
    {
        foreach (var line in Lines)
        {
            var refs = state.Cells.GetEmptyReferences();
            foreach (var branch in line.Branches)
            {
                foreach (GridCoord cell in branch.Cells)
                {
                    refs.Include(in state.Cells[cell]);
                }
            }

            yield return refs.Box();
        }
    }

    public override void SaveToJsonObject(JsonObject obj)
    {
    }
}

