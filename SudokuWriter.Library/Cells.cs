using System;
using System.Numerics;

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

    public int GetSingle(int row, int col)
    {
        ushort value = _cells[row, col];
        if (value == 0)
        {
            return -1;
        }

        if (!BitOperations.IsPow2(value))
        {
            return -1;
        }

        return BitOperations.Log2(value);
    }

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

    public bool TryRemoveDigit(int row, int column, int digit, out Cells removed)
    {
        ushort c = _cells[row, column];
        var mask = unchecked((ushort)(1 << digit));
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

    public static bool IsDigitSet(ushort mask, int digit)
    {
        return (mask & (1 << digit)) != 0;
    }
    
    public static ushort GetDigitMask(int digit)
    {
        return (ushort)(1 << digit);
    }

    public static Cells CreateFilled(GameStructure structure) =>
        CreateFilled(structure.Rows, structure.Columns, structure.Digits);
}