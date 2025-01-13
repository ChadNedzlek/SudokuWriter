using System;
using System.IO.Hashing;
using System.Runtime.InteropServices;

namespace SudokuWriter.Library;

public readonly struct ImmutableArray2<T>
    where T : struct
{
    public int Rows { get; }
    public int Columns { get; }
    private readonly ReadOnlyMemory<T> _array;

    public ImmutableArray2(int rows, int columns)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        Rows = rows;
        Columns = columns;
        _array = new T[rows * columns];
    }

    public ImmutableArray2(ReadOnlyMemory<T> array, int columns)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        Rows = array.Length / columns;
        ArgumentOutOfRangeException.ThrowIfNotEqual(Rows * columns, array.Length, nameof(array));
        Columns = columns;
        _array = array;
    }

    private int CalcIndex(int row, int col)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Columns);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);
        return row * Columns + col;
    }

    public T this[int row, int col] => _array.Span[CalcIndex(row, col)];

    public ImmutableArray2.Builder<T> ToBuilder()
    {
        var array = new T[_array.Length];
        _array.CopyTo(array);
        return ImmutableArray2.Builder<T>.EncapsulateArray(array, Columns);
    }

    public ImmutableArray2<T> SetItem(int row, int column, T value)
    {
        var array = new T[_array.Length];
        _array.CopyTo(array);
        array[CalcIndex(row, column)] = value;
        return new ImmutableArray2<T>(array, Columns);
    }

    public ulong GetHash64()
    {
        return XxHash64.HashToUInt64(MemoryMarshal.AsBytes(_array.Span));
    }
}

public static class ImmutableArray2
{
    public static ImmutableArray2<T> Create<T>(int rows, int columns)
        where T : struct
    {
        return new ImmutableArray2<T>(rows, columns);
    }

    public static Builder<T> CreateBuilder<T>(int rows, int columns)
        where T : struct
    {
        return new ImmutableArray2<T>(rows, columns).ToBuilder();
    }

    public static Builder<T> FromMemory<T>(Memory<T> array, int columns)
        where T : struct
    {
        return Builder<T>.EncapsulateArray(array, columns);
    }

    public class Builder<T>
        where T : struct
    {
        private readonly Memory<T> _array;

        private bool _disposed;

        private Builder(Memory<T> array, int columns)
        {
            Columns = columns;
            Rows = array.Length / Columns;
            _array = array;
        }

        public int Rows { get; }
        public int Columns { get; }

        public ref T this[int row, int col]
        {
            get
            {
                ThrowIfDisposed();
                return ref _array.Span[CalcIndex(row, col)];
            }
        }

        public static Builder<T> EncapsulateArray(Memory<T> array, int columns)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
            ArgumentOutOfRangeException.ThrowIfZero(array.Length, nameof(array));
            ArgumentOutOfRangeException.ThrowIfNotEqual(array.Length / columns * columns, array.Length, nameof(array));
            return new Builder<T>(array, columns);
        }

        private int CalcIndex(int row, int col)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(col, Columns);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);
            return row * Columns + col;
        }

        public ImmutableArray2<T> MoveToImmutable()
        {
            ThrowIfDisposed();
            return new ImmutableArray2<T>(_array, Columns);
        }

        public void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        public void Fill(T value)
        {
            _array.Span.Fill(value);
        }
    }
}