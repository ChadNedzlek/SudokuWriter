using System;

namespace SudokuWriter.Library;

public readonly struct CellsBuilder
{
    public int Rows => _cells.Rows;
    public int Columns => _cells.Columns;

    private readonly ImmutableArray2.Builder<ushort> _cells;

    public CellsBuilder(ImmutableArray2.Builder<ushort> cells)
    {
        _cells = cells;
    }

    public bool this[int row, int col, ushort digit]
    {
        get => ((_cells[row, col] >> digit) & 1) != 0;
        set => _cells[row, col] |= unchecked((ushort)(value ? 1 << digit : 0));
    }

    public bool IsInRange(int row, int col) => row > 0 && row <= Rows && col > 0 && col <= Columns;
    public bool IsInRange(GridCoord coord, GridOffset offset) => IsInRange(coord.Row + offset.Row, coord.Col + offset.Col);
    public bool IsInRange(int row, int col, GridOffset offset) => IsInRange(row + offset.Row, col + offset.Col);

    public ref ushort this[int row, int col] => ref _cells[row, col];
    public ref ushort this[(int row, int col) coord] => ref _cells[coord.row, coord.col];

    public MultiRef<ushort> GetEmptyReferences() => _cells.GetEmptyReference();
    public MultiRef<ushort> GetRow(int row) => _cells.GetRowReference(row);
    public MultiRef<ushort> GetColumn(int columns) => _cells.GetColumnReference(columns);
    public MultiRef<ushort> GetRange(Range rows, Range columns) => _cells.GetRectangle(rows, columns);

    public Cells MoveToImmutable()
    {
        return new Cells(_cells.MoveToImmutable());
    }

    public void SetSingle(int row, int column, ushort value)
    {
        _cells[row, column] = Cells.GetDigitMask(value);
    }

    public int GetSingle(int row, int column) => Cells.GetSingle(_cells[row, column]);
}