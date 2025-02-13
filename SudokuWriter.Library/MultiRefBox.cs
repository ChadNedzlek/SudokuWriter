using System;

namespace VaettirNet.SudokuWriter.Library;

public interface IOffsetContainer
{
    int GetOffsets(scoped Span<ushort> offsets);
    static abstract int MaxCount { get; }
}

public readonly struct MultiRefBox<T> : IOffsetContainer
{
    public readonly OffsetList Offsets;
    public readonly int Length;

    internal MultiRefBox(OffsetList offsets, int length)
    {
        Offsets = offsets;
        Length = length;
    }

    public MultiRef<T> Unbox(Span<T> references) => new(references, Offsets, Length);
    public ReadOnlyMultiRef<T> Unbox(ReadOnlySpan<T> references) => new(references, Offsets, Length);

    public int GetOffsets(Span<ushort> offsets)
    {
        int c = 0;
        for (int ip = 0; ip < Length; ip++)
        {
            ulong ptr = Offsets[ip];
            if (ptr <= int.MaxValue)
            {
                offsets[c++] = (ushort)ptr;
                continue;
            }

            ushort start = unchecked((ushort)(ptr >> 48));
            ushort runLength = unchecked((ushort)(ptr >> 32));
            ushort strideLength = unchecked((ushort)(ptr >> 16));
            ushort strideCount = unchecked((ushort)ptr);
            for (int iStride = 0; iStride < strideCount; iStride++)
            for (int i = 0; i < runLength; i++)
                offsets[c++] = (ushort)(iStride * strideLength + i + start);
        }

        return c;
    }

    public static int MaxCount => OffsetList.Size;
}

public static class OffsetContainer
{
    public static bool IsStrictSuperSetOf<TSuperset, TTest>(this TSuperset self, TTest other) where TSuperset : IOffsetContainer, allows ref struct
        where TTest : IOffsetContainer, allows ref struct
    {
        Span<ushort> superSet = stackalloc ushort[TSuperset.MaxCount];
        Span<ushort> testSet = stackalloc ushort[TTest.MaxCount];
        superSet = superSet[..self.GetOffsets(superSet)];
        testSet = testSet[..other.GetOffsets(testSet)];
        
        if (testSet.Length >= superSet.Length) return false;

        if (!superSet.ContainsAny(testSet)) return false;

        foreach (var item in testSet)
        {
            if (!superSet.Contains(item)) return false;
        }

        return true;
    }
}