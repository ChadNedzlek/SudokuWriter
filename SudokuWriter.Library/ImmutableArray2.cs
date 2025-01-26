using System;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VaettirNet.SudokuWriter.Library;

public readonly struct ImmutableArray2<T>
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

    public ref readonly T this[int row, int col] => ref _array.Span[CalcIndex(row, col)];

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

    public ReadOnlyMultiRef<T> GetEmptyReference() => new(_array.Span);

    public ReadOnlyMultiRef<T> GetColumnReference(int column)
    {
        ReadOnlyMultiRef<T> refs = new ReadOnlyMultiRef<T>(_array.Span);
        checked
        {
            refs.IncludeStrides((ushort)CalcIndex(0, column), 1, (ushort)Columns, (ushort)Rows);
        }
        return refs;
    }

    public ReadOnlyMultiRef<T> GetRowReference(int row)
    {
        ReadOnlyMultiRef<T> refs = new ReadOnlyMultiRef<T>(_array.Span);
        checked
        {
            refs.IncludeStride((ushort)CalcIndex(row, 0), (ushort)Rows);
        }
        return refs;
    }

    public ReadOnlyMultiRef<T> GetRectangle(Range rows, Range columns)
    {
        var (startRow, numRows) = rows.GetOffsetAndLength(Rows);
        var (startCol, numCols) = columns.GetOffsetAndLength(Columns);
        ReadOnlyMultiRef<T> refs = new ReadOnlyMultiRef<T>(_array.Span);
        checked
        {
            refs.IncludeStrides((ushort)CalcIndex(startRow, startCol), (ushort)numCols, (ushort)Columns, (ushort)numRows);
        }
        return refs;
    }

    public ReadOnlyMultiRef<T> Unbox(MultiRefBox<T> box) => box.Unbox(_array.Span);

    public ulong GetHash64()
    {
        unsafe
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                throw new NotSupportedException();
            }

            ReadOnlySpan<T> span = _array.Span;
            
            Span<byte> bytes = MemoryMarshal.CreateSpan(
                ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(span)),
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
                checked(span.Length * sizeof(T))
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
            );

            return XxHash64.HashToUInt64(bytes);
        }
    }

    public static ImmutableArray2<T> FromValues(T[,] values)
    {
        var flat = new T[values.Length];
        Array.Copy(values, flat, flat.Length);
        return new ImmutableArray2<T>(flat, values.GetLength(1));
    }
}

public static class ImmutableArray2
{
    public static ImmutableArray2<T> Create<T>(int rows, int columns)
    {
        return new ImmutableArray2<T>(rows, columns);
    }
    public static ImmutableArray2<T> Create<T>(T[,] values)
    {
        return ImmutableArray2<T>.FromValues(values);
    }

    public static Builder<T> CreateBuilder<T>(int rows, int columns)
    {
        return new ImmutableArray2<T>(rows, columns).ToBuilder();
    }

    public static Builder<T> FromMemory<T>(Memory<T> array, int columns)
    {
        return Builder<T>.EncapsulateArray(array, columns);
    }

    public class Builder<T>
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
            _disposed = true;
            return new ImmutableArray2<T>(_array, Columns);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        public void Fill(T value)
        {
            ThrowIfDisposed();
            _array.Span.Fill(value);
        }

        public MultiRef<T> GetEmptyReference()
        {
            ThrowIfDisposed();
            return new MultiRef<T>(_array.Span);
        }

        public MultiRef<T> GetColumnReference(int column)
        {
            ThrowIfDisposed();
            MultiRef<T> refs = new MultiRef<T>(_array.Span);
            checked
            {
                refs.IncludeStrides((ushort)CalcIndex(0, column), 1, (ushort)Columns, (ushort)Rows);
            }
            return refs;
        }

        public MultiRef<T> GetRowReference(int row)
        {
            ThrowIfDisposed();
            MultiRef<T> refs = new MultiRef<T>(_array.Span);
            checked
            {
                refs.IncludeStride((ushort)CalcIndex(row, 0), (ushort)Rows);
            }
            return refs;
        }

        public MultiRef<T> GetRectangle(Range rows, Range columns)
        {
            ThrowIfDisposed();
            var (startRow, numRows) = rows.GetOffsetAndLength(Rows);
            var (startCol, numCols) = columns.GetOffsetAndLength(Columns);
            MultiRef<T> refs = new MultiRef<T>(_array.Span);
            checked
            {
                refs.IncludeStrides((ushort)CalcIndex(startRow, startCol), (ushort)numCols, (ushort)Columns, (ushort)numRows);
            }
            return refs;
        }

        public MultiRef<T> Unbox(MultiRefBox<T> box) => box.Unbox(_array.Span);
    }
}