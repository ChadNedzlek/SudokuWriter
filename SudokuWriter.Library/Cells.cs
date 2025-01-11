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
        var value = _cells[row, col];
        if (value == 0) return -1;
        if (!BitOperations.IsPow2(value)) return -1;
        return BitOperations.Log2(value);
    }

    public CellsBuilder ToBuilder() => new(_cells.ToBuilder());

    public static Cells CreateFilled(int rows = 9, int columns = 9, int digits = 9)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(digits);
        var array = ImmutableArray2.CreateBuilder<ushort>(rows, columns);
        array.Fill(unchecked((ushort)((1 << digits) - 1)));
        return new Cells(array.MoveToImmutable());
    }

    public Cells SetCell(int row, int column, int digit) => new(_cells.SetItem(row, column, unchecked((ushort)(1 << digit))));
    public ushort GetMask(int row, int column) => _cells[row, column];
}