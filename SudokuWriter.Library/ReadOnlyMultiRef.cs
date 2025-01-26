using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SudokuWriter.Library;

public readonly ref struct ReadOnlyMultiRef<T>
{
    private readonly ReadOnlySpan<T> _ref;
    private readonly List<ulong> _offsets;
    
    public ReadOnlyMultiRef(ReadOnlySpan<T> validSpace)
    {
        _offsets = new List<ulong>(10);
        _ref = validSpace;
    }

    internal ReadOnlyMultiRef(ReadOnlySpan<T> validSpace, List<ulong> offsets)
    {
        _ref = validSpace;
        _offsets = offsets;
    }

    public int GetCount()
    {
        int cnt = 0;
        foreach (var ptr in _offsets)
        {
            if (ptr <= ushort.MaxValue)
            {
                cnt++;
                continue;
            }
            
            ushort runLength = unchecked((ushort)(ptr >> 32));
            ushort strideCount = unchecked((ushort)ptr);
            cnt += runLength * strideCount;
        }

        return cnt;
    }

    public void Include(ref readonly T target)
    {
        if (!_ref.Overlaps(new ReadOnlySpan<T>(in target), out int offset))
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

    public void ForEach(Action<T> callback)
    {
        foreach (ulong ptr in _offsets)
        {
            if (ptr <= int.MaxValue)
            {
                callback(_ref[(int)ptr]);
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
                    callback(_ref[iStride * strideLength + i + start]);
                }
            }
        }
    }

    public TOut Aggregate<TOut>(Func<TOut, T, TOut> callback) => Aggregate(default, callback);

    public TOut Aggregate<TOut>(TOut seed, Func<TOut, T, TOut> callback)
    {
        foreach (ulong ptr in _offsets)
        {
            if (ptr <= int.MaxValue)
            {
                seed = callback(seed, _ref[(int)ptr]);
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
                    seed = callback(seed, _ref[iStride * strideLength + i + start]);
                }
            }
        }

        return seed;
    }

    public TOut Aggregate<TOut, TContext>(Func<TOut, T, TContext, TOut> callback, TContext context)
        where TContext : allows ref struct => Aggregate(default, callback, context);

    public TOut Aggregate<TOut, TContext>(TOut seed, Func<TOut, T, TContext, TOut> callback, TContext context)
        where TContext : allows ref struct
    {
        foreach (ulong ptr in _offsets)
        {
            if (ptr <= int.MaxValue)
            {
                seed = callback(seed, _ref[(int)ptr], context);
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
                    seed = callback(seed, _ref[iStride * strideLength + i + start], context);
                }
            }
        }

        return seed;
    }

    public ImmutableArray<T> ToImmutableList()
    {
        var values = ImmutableArray.CreateBuilder<T>(20);
        foreach (ulong ptr in _offsets)
        {
            if (ptr <= int.MaxValue)
            {
                values.Add(_ref[(int)ptr]);
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
                    values.Add(_ref[iStride * strideLength + i + start]);
                }
            }
        }

        return values.ToImmutable();
    }

    public MultiRefBox<T> Box() => new(_offsets);
}