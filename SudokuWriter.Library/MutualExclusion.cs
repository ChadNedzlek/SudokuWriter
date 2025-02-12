using System;
using System.Collections.Immutable;
using System.Numerics;

namespace VaettirNet.SudokuWriter.Library;

public static class MutualExclusion
{
    public static bool ApplyMutualExclusionRules(MultiRef<CellValueMask> cells)
    {
        ImmutableArray<CellValueMask> cellValues = cells.ToImmutableList();
        Span<CellValueMask> allowedMasks = stackalloc CellValueMask[16];
        allowedMasks.Fill(CellValueMask.All(15));
        ushort maxMask = (ushort)(1 << cellValues.Length);
        for (ushort comboMask = 1; comboMask < maxMask - 2; comboMask++)
        {
            CellValueMask setMask = CellValueMask.None;
            for (ushort mask = 1, i=0; mask <= comboMask; mask <<= 1, i++)
            {
                if ((comboMask & mask) != 0) setMask |= cellValues[i];
            }

            if (BitOperations.PopCount(comboMask) == setMask.Count)
            {
                CellValueMask allowed = ~setMask;
                for (ushort mask = 1, i=0; i< cellValues.Length; mask <<= 1, i++)
                {
                    if ((~comboMask & mask) != 0) allowedMasks[i] &= allowed;
                }
            }
        }

        return ApplyMasks(cells, allowedMasks);
    }

    private static bool ApplyMasks(MultiRef<CellValueMask> cells, ReadOnlySpan<CellValueMask> allowedMasks)
    {
        int iSet = 0;

        bool ApplyMask(bool r, scoped ref CellValueMask cell, ReadOnlySpan<CellValueMask> mask) => r | RuleHelpers.TryMask(ref cell, mask[iSet++]);

        return cells.Aggregate<bool, ReadOnlySpan<CellValueMask>>(ApplyMask, allowedMasks);
    }
}