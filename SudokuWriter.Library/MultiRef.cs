using System;
using System.Collections.Generic;

namespace SudokuWriter.Library;

public readonly ref struct MultiRef<T>
{
    private readonly Span<T> _ref;
    private readonly List<ulong> _offsets;

    public MultiRef(Span<T> validSpace)
    {
        _offsets = new List<ulong>(10);
        _ref = validSpace;
    }

    internal MultiRef(Span<T> validSpace, List<ulong> offsets)
    {
        _ref = validSpace;
        _offsets = offsets;
    }

    public void Include(ref T target)
    {
        if (!_ref.Overlaps(new ReadOnlySpan<T>(ref target), out int offset))
        {
            throw new ArgumentException("target is not contained in this MultiRef", nameof(target));
        }

        _offsets.Add((ulong)offset);
    }
    
    public void Include(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _ref.Length);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        _offsets.Add((ulong)index);
    }

    public void IncludeStrides(ushort start, ushort runLength, ushort strideLength, ushort strideCount)
    {
        _offsets.Add((ulong)start << 48 | (ulong)runLength << 32 | (ulong)strideLength << 16 | strideCount);
    }

    public void IncludeStride(ushort start, ushort runLength)
    {
        _offsets.Add((ulong)start << 48 | (ulong)runLength << 32 | 1);
    }

    public void ForEach(RefAction<T> callback)
    {
        foreach (ulong ptr in _offsets)
        {
            if (ptr <= int.MaxValue)
            {
                callback(ref _ref[(int)ptr]);
                continue;
            }

            ushort start = unchecked((ushort)(ptr >> 48));
            ushort runLength = unchecked((ushort)(ptr >> 32));
            ushort strideLength = unchecked((ushort)(ptr >> 16));
            ushort strideCount = unchecked((ushort)ptr);
            for (int iStride = 0; iStride < strideCount; iStride++)
            {
                for (int i = 0; i < runLength; i++)
                {
                    callback(ref _ref[iStride * strideLength + i + start]);
                }
            }
        }
    }

    public TOut Aggregate<TOut>(RefAggregator<T, TOut> callback) => Aggregate(default, callback);

    public TOut Aggregate<TOut>(TOut seed, RefAggregator<T, TOut> callback)
    {
        foreach (ulong ptr in _offsets)
        {
            if (ptr <= int.MaxValue)
            {
                seed = callback(seed, ref _ref[(int)ptr]);
                continue;
            }

            ushort start = unchecked((ushort)(ptr >> 48));
            ushort runLength = unchecked((ushort)(ptr >> 32));
            ushort strideLength = unchecked((ushort)(ptr >> 16));
            ushort strideCount = unchecked((ushort)ptr);
            for (int iStride = 0; iStride < strideCount; iStride++)
            {
                for (int i = 0; i < runLength; i++)
                {
                    seed = callback(seed, ref _ref[iStride * strideLength + i + start]);
                }
            }
        }

        return seed;
    }

    public MultiRefBox<T> Box() => new(_offsets);
}

public delegate void RefAction<T>(scoped ref T value);
public delegate TOut RefAggregator<TIn, TOut>(TOut input, scoped ref TIn value);

public readonly struct MutexGroup
{
    public MutexGroup(MultiRefBox<ushort> cells)
    {
        Cells = cells;
    }

    public MultiRefBox<ushort> Cells { get; }
}

public static class MutualExclusion
{
}