using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SudokuWriter.Library;

public readonly struct Cells
{
    private readonly ImmutableArray2<ushort> _cells;

    public Cells(ImmutableArray2<ushort> cells)
    {
        _cells = cells;
    }

    public int Rows => _cells.Rows;
    public int Columns => _cells.Columns;

    public bool this[int row, int col, int digit] => ((_cells[row, col] >> digit) & 1) != 0;
    public ushort this[int row, int col] => _cells[row, col];
    public ushort this[GridCoord coord] => _cells[coord.Row, coord.Col];

    public int GetSingle(int row, int col) => GetSingle(_cells[row, col]);

    public CellsBuilder ToBuilder()
    {
        return new CellsBuilder(_cells.ToBuilder());
    }

    public static Cells CreateFilled(int rows = 9, int columns = 9, int digits = 9)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(digits);
        ImmutableArray2.Builder<ushort> array = ImmutableArray2.CreateBuilder<ushort>(rows, columns);
        array.Fill(unchecked((ushort)((1 << digits) - 1)));
        return new Cells(array.MoveToImmutable());
    }

    public Cells SetCell(int row, int column, int digit)
    {
        return new Cells(_cells.SetItem(row, column, unchecked((ushort)(1 << digit))));
    }

    public Cells FillEmpty(GameStructure structure)
    {
        ImmutableArray2.Builder<ushort> b = _cells.ToBuilder();
        for (int r = 0; r < b.Rows; r++)
        for (int c = 0; c < b.Columns; c++)
        {
            ref ushort cell = ref b[r, c];
            if (cell == 0) cell = GetAllDigitsMask(structure.Digits);
        }

        return new Cells(b.MoveToImmutable());
    }

    public bool TryRemoveDigit(int row, int column, int digit, out Cells removed)
    {
        ushort c = _cells[row, column];
        ushort mask = unchecked((ushort)(1 << digit));
        if ((c & mask) == 0)
        {
            removed = default;
            return false;
        }

        c = unchecked((ushort)(c & (ushort)~mask));
        if (c == 0)
        {
            removed = default;
            return false;
        }

        removed = new Cells(_cells.SetItem(row, column, c));
        return true;
    }

    public ushort GetMask(int row, int column)
    {
        return _cells[row, column];
    }

    public ulong GetCellHash()
    {
        return _cells.GetHash64();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDigitSet(ushort mask, int digit)
    {
        return (mask & (1 << digit)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort GetDigitMask(int digit)
    {
        return (ushort)(1 << digit);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort GetAllDigitsMask(int digits)
    {
        return unchecked((ushort)((1 << digits) - 1));
    }

    public static Cells CreateFilled(GameStructure structure)
    {
        return CreateFilled(structure.Rows, structure.Columns, structure.Digits);
    }

    public const int NoSingleValue = -1;

    public static bool IsSingle(ushort mask)
    {
        return mask == 0 || BitOperations.IsPow2(mask);
    }

    public static int GetSingle(ushort mask)
    {
        return IsSingle(mask) ? BitOperations.Log2(mask) : NoSingleValue;
    }

    public static Cells FromMasks(ushort[,] masks)
    {
        ushort[] cells = new ushort[masks.Length];
        ReadOnlySpan<ushort> span = MemoryMarshal.Cast<byte, ushort>(MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(masks), sizeof(ushort) * masks.Length));
        span.CopyTo(cells);
        return new Cells(new ImmutableArray2<ushort>(cells, masks.GetLength(1)));
    }

    public static Cells FromDigits(ushort[,] digits)
    {
        ushort[] cells = new ushort[digits.Length];
        ReadOnlySpan<ushort> span = MemoryMarshal.Cast<byte, ushort>(MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(digits), sizeof(ushort) * digits.Length));
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = GetDigitMask(span[i]);
        }
        return new Cells(new ImmutableArray2<ushort>(cells, digits.GetLength(1)));
    }

    public static string GetDigitDisplay(ushort mask)
    {
        StringBuilder b = new();
        for (int i = 0; (1 << i) <= mask; i++)
        {
            if (((1 << i) & mask) != 0)
            {
                b.Append((char)('1' + i));
            }
        }

        return b.ToString();
    }

    public static ushort GetReversed(ushort mask, ushort digits)
    {
        unchecked
        {
            uint x = mask;
            x |= (x & 0x000000FF) << 16;
            x = (x & 0xF0F0F0F0) | ((x & 0x0F0F0F0F) << 8);
            x = (x & 0xCCCCCCCC) | ((x & 0x33333333) << 4);
            x = (x & 0XAAAAAAAA) | ((x & 0x55555555) << 2);
            x <<= 1;
            x >>= 32 - digits;
            return (ushort)x;
        }
    }
}