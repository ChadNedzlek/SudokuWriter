using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VaettirNet.SudokuWriter.Library;

[InlineArray(10)]
public struct OffsetList
{
    private ulong _element0;

    public static unsafe int Size { get; } = sizeof(OffsetList) / sizeof(ulong);
}

[DebuggerDisplay("{Render(),nq}")]
public ref struct MultiRef<T> : IOffsetContainer
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
        for(int i = length; i < length; i++)
        {
            if (_offsets[i] != offsets[i])
            {
                throw new ArgumentException();
            }
        }
    }
    
    public readonly bool IsEmpty() => _length == 0;

    public void Clear() => _length = 0;

    public void UnboxFrom(MultiRefBox<T> input) => this = new MultiRef<T>(_ref, input.Offsets, input.Length);

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
        for (int ip = 0; ip < _length; ip++)
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
        for (int ip = 0; ip < _length; ip++)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlyMultiRef<T>(in MultiRef<T> v) => new(v._ref, v._offsets, v._length);

    public void Except<TExcept>(TExcept except) where TExcept : IOffsetContainer, allows ref struct
    {
        var offsets = MemoryMarshal.CreateSpan(ref Unsafe.As<OffsetList, long>(ref _offsets), _length);
        
        if (offsets.ContainsAnyExceptInRange(0, ushort.MaxValue))
        {
            // There's a weird stride one in here, so we need to clear it out
            Span<ushort> tmp = stackalloc ushort[MaxCount];
            tmp = tmp[..GetOffsets(tmp)];
            _length = tmp.Length;
            for (int i = 0; i < tmp.Length; i++)
            {
                _offsets[i] = tmp[i];
            }
            offsets = MemoryMarshal.CreateSpan(ref Unsafe.As<OffsetList, long>(ref _offsets), _length);
        }

        Span<ushort> exceptOffsets = stackalloc ushort[TExcept.MaxCount];
        exceptOffsets = exceptOffsets[..except.GetOffsets(exceptOffsets)];

        for (int i = 0; i < _length; i++)
        {
            if (exceptOffsets.Contains((ushort)offsets[i]))
            {
                offsets[i] = offsets[--_length];
                i--;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetOffsets(scoped Span<ushort> offsets) => ((ReadOnlyMultiRef<T>)this).GetOffsets(offsets);

    public static int MaxCount => OffsetList.Size;

    public string Render()
    {
        return Aggregate("", (string s, scoped ref T v, MultiRef<T> t) => s + $"{t.Render(in v)}, ", this).TrimEnd(' ', ',');
    }
    
    private string Render(scoped ref readonly T x)
    {
        int nRow = (int)IntMath.Sqrt((uint)_ref.Length);
        int nCol = _ref.Length / nRow;
        _ref.Overlaps(new ReadOnlySpan<T>(in x), out int offset);
        int r = offset / nCol;
        int c = offset - (r * nCol);
        return $"({r},{c}) {x}";
    }
}

public static class IntMath
{
    public static uint Sqrt(uint x)
    {
        uint m, y, b;
        m = 0x40000000;
        y = 0;
        while (m != 0)
        {
            b = y | m;
            y >>= 1;
            if (x >= b)
            {
                x -= b;
                y |= m;
            }

            m >>= 2;
        }

        return y;
    }
}

public delegate void RefAction<T>(scoped ref T value);
public delegate void ReadonlyRefContextAction<T, in TContext>(scoped ref readonly T value, TContext ctx)
    where TContext : allows ref struct;
public delegate void ReadonlyRefContextRefAction<T, TContext>(scoped ref readonly T value, scoped ref TContext ctx)
    where TContext : allows ref struct;

public delegate TOut RefAggregator<TIn, TOut>(TOut input, scoped ref TIn value);

public delegate TOut RefContextAggregator<TIn, TOut, in TContext>(TOut input, scoped ref TIn value, TContext ctx)
    where TContext : allows ref struct;

public delegate TOut ReadonlyRefContextAggregator<TIn, TOut, in TContext>(TOut input, scoped ref readonly TIn value, TContext ctx)
    where TContext : allows ref struct;