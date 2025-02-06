using System;
using System.Collections.Immutable;

namespace VaettirNet.SudokuWriter.Library;

public ref struct ReadOnlyMultiRef<T>
{
    private readonly ReadOnlySpan<T> _ref;
    private OffsetList _offsets;
    private int _length;

    public ReadOnlyMultiRef(ReadOnlySpan<T> validSpace)
    {
        _ref = validSpace;
    }

    internal ReadOnlyMultiRef(ReadOnlySpan<T> validSpace, OffsetList offsets, int length)
    {
        _ref = validSpace;
        _offsets = offsets;
        _length = length;
    }

    private void Add(ulong o)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(_length, 9);
        _offsets[_length++] = o;
    }

    public int GetCount()
    {
        int cnt = 0;
        for (int ip=0;ip<_length;ip++)
        {
            ulong ptr = _offsets[ip];
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

        Add((ulong)offset);
    }
    
    public void Include(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _ref.Length);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        Add((ulong)index);
    }

    public void IncludeStrides(ushort start, ushort runLength, ushort strideLength, ushort strideCount)
    {
        Add((ulong)start << 48 | (ulong)runLength << 32 | (ulong)strideLength << 16 | strideCount);
    }

    public void IncludeStride(ushort start, ushort runLength)
    {
        Add((ulong)start << 48 | (ulong)runLength << 32 | 1);
    }

    public readonly void ForEach(Action<T> callback)
    {
        for (int ip=0;ip<_length;ip++)
        {
            ulong ptr = _offsets[ip];
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

    public readonly TOut Aggregate<TOut>(Func<TOut, T, TOut> callback) => Aggregate(default, callback);

    public readonly TOut Aggregate<TOut>(TOut seed, Func<TOut, T, TOut> callback)
    {
        for (int ip=0;ip<_length;ip++)
        {
            ulong ptr = _offsets[ip];
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

    public readonly TOut Aggregate<TOut, TContext>(Func<TOut, T, TContext, TOut> callback, TContext context)
        where TContext : allows ref struct => Aggregate(default, callback, context);

    public readonly TOut Aggregate<TOut, TContext>(TOut seed, Func<TOut, T, TContext, TOut> callback, TContext context)
        where TContext : allows ref struct
    {
        for (int ip=0;ip<_length;ip++)
        {
            ulong ptr = _offsets[ip];
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

    public readonly ImmutableArray<T> ToImmutableList()
    {
        var values = ImmutableArray.CreateBuilder<T>(20);
        for (int ip=0;ip<_length;ip++)
        {
            ulong ptr = _offsets[ip];
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

    public readonly MultiRefBox<T> Box() => new(_offsets, _length);
}