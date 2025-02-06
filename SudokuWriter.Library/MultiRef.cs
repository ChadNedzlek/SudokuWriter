using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace VaettirNet.SudokuWriter.Library;

[InlineArray(10)]
internal struct OffsetList
{
    private ulong _element0;
}

public ref struct MultiRef<T>
{
    private readonly Span<T> _ref;
    private OffsetList _offsets;
    private int _length;

    public MultiRef(Span<T> validSpace)
    {
        _ref = validSpace;
    }

    internal MultiRef(Span<T> validSpace, OffsetList offsets, int length)
    {
        _ref = validSpace;
        _offsets = offsets;
        _length = length;
    }

    public readonly int GetCount()
    {
        int cnt = 0;
        foreach (ulong ptr in _offsets)
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

    private void Add(ulong o)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(_length, 9);
        _offsets[_length++] = o;
    }

    public void Include(ref T target)
    {
        if (!_ref.Overlaps(new ReadOnlySpan<T>(ref target), out int offset))
            throw new ArgumentException("target is not contained in this MultiRef", nameof(target));

        if (_length > 9) throw new ArgumentOutOfRangeException();
        Add((ulong)offset);
    }

    public void Include(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _ref.Length);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        if (_length > 9) throw new ArgumentOutOfRangeException();
        Add((ulong)index);
    }

    public void IncludeStrides(ushort start, ushort runLength, ushort strideLength, ushort strideCount)
    {
        if (_length > 9) throw new ArgumentOutOfRangeException();
        Add(((ulong)start << 48) | ((ulong)runLength << 32) | ((ulong)strideLength << 16) | strideCount);
    }

    public void IncludeStride(ushort start, ushort runLength)
    {
        Add(((ulong)start << 48) | ((ulong)runLength << 32) | 1);
    }

    public readonly void ForEach(RefAction<T> callback)
    {
        for (int ip=0;ip<_length;ip++)
        {
            ulong ptr = _offsets[ip];
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
            for (int i = 0; i < runLength; i++)
                callback(ref _ref[iStride * strideLength + i + start]);
        }
    }

    public readonly TOut Aggregate<TOut>(RefAggregator<T, TOut> callback)
    {
        return Aggregate(default, callback);
    }

    public readonly TOut Aggregate<TOut>(TOut seed, RefAggregator<T, TOut> callback)
    {
        for (int ip = 0; ip < _length; ip++)
        {
            ulong ptr = _offsets[ip];
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
            for (int i = 0; i < runLength; i++)
                seed = callback(seed, ref _ref[iStride * strideLength + i + start]);
        }

        return seed;
    }

    public readonly TOut Aggregate<TOut, TContext>(RefContextAggregator<T, TOut, TContext> callback, TContext context)
        where TContext : allows ref struct
    {
        return Aggregate(default, callback, context);
    }

    public readonly TOut Aggregate<TOut, TContext>(TOut seed, RefContextAggregator<T, TOut, TContext> callback, TContext context)
        where TContext : allows ref struct
    {
        for (int ip=0;ip<_length;ip++)
        {
            ulong ptr = _offsets[ip];
            if (ptr <= int.MaxValue)
            {
                seed = callback(seed, ref _ref[(int)ptr], context);
                continue;
            }

            ushort start = unchecked((ushort)(ptr >> 48));
            ushort runLength = unchecked((ushort)(ptr >> 32));
            ushort strideLength = unchecked((ushort)(ptr >> 16));
            ushort strideCount = unchecked((ushort)ptr);
            for (int iStride = 0; iStride < strideCount; iStride++)
            for (int i = 0; i < runLength; i++)
                seed = callback(seed, ref _ref[iStride * strideLength + i + start], context);
        }

        return seed;
    }

    public readonly ImmutableArray<T> ToImmutableList()
    {
        ImmutableArray<T>.Builder values = ImmutableArray.CreateBuilder<T>(20);
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
            for (int i = 0; i < runLength; i++)
                values.Add(_ref[iStride * strideLength + i + start]);
        }

        return values.ToImmutable();
    }

    public readonly MultiRefBox<T> Box()
    {
        return new MultiRefBox<T>(_offsets, _length);
    }
}

public delegate void RefAction<T>(scoped ref T value);

public delegate TOut RefAggregator<TIn, TOut>(TOut input, scoped ref TIn value);

public delegate TOut RefContextAggregator<TIn, TOut, TContext>(TOut input, scoped ref TIn value, TContext ctx)
    where TContext : allows ref struct;