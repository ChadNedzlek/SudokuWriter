using System;
using System.Collections.Immutable;
using System.Numerics;

namespace VaettirNet.SudokuWriter.Library;

public static class MutualExclusion
{
    public static bool ApplyMutualExclusionRules(MultiRef<ushort> cells)
    {
        ImmutableArray<ushort> cellValues = cells.ToImmutableList();
        Span<ushort> allowedMasks = stackalloc ushort[16];
        allowedMasks.Fill(ushort.MaxValue);
        ushort maxMask = (ushort)(1 << cellValues.Length);
        for (ushort comboMask = 1; comboMask < maxMask - 2; comboMask++)
        {
            ushort setMask = 0;
            for (ushort mask = 1, i=0; mask <= comboMask; mask <<= 1, i++)
            {
                if ((comboMask & mask) != 0) setMask |= cellValues[i];
            }

            if (BitOperations.PopCount(comboMask) == BitOperations.PopCount(setMask))
            {
                ushort allowed = (ushort)~setMask;
                for (ushort mask = 1, i=0; i< cellValues.Length; mask <<= 1, i++)
                {
                    if ((~comboMask & mask) != 0) allowedMasks[i] &= allowed;
                }
            }
        }

        int iSet = 0;
        return cells.Aggregate(
            (bool r, scoped ref ushort cell, ReadOnlySpan<ushort> mask) => r | RuleHelpers.TryMask(ref cell, mask[iSet++]),
            allowedMasks
        );
    }
}