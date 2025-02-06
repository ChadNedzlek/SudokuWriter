using System;

namespace VaettirNet.SudokuWriter.Library;

public readonly struct CellsBuilder
{
    public int Rows => _cells.Rows;
    public int Columns => _cells.Columns;

    private readonly ImmutableArray2.Builder<CellValueMask> _cells;

    public CellsBuilder(ImmutableArray2.Builder<CellValueMask> cells)
    {
        _cells = cells;
    }

    public bool this[int row, int col, CellValue digit]
    {
        get => _cells[row, col].Contains(digit);
        set
        {
            ref CellValueMask c = ref _cells[row, col];
            if (value)
                c |= digit.AsMask();
            else
                c &= ~digit.AsMask();
        }
    }

    public bool IsInRange(int row, int col) => row > 0 && row < Rows && col > 0 && col < Columns;
    public bool IsInRange(GridCoord coord, GridOffset offset) => IsInRange(coord.Row + offset.Row, coord.Col + offset.Col);
    public bool IsInRange(int row, int col, GridOffset offset) => IsInRange(row + offset.Row, col + offset.Col);

    public ref CellValueMask this[int row, int col] => ref _cells[row, col];
    public ref CellValueMask this[(int row, int col) coord] => ref _cells[coord.row, coord.col];

    public MultiRef<CellValueMask> GetEmptyReferences() => _cells.GetEmptyReference();
    public MultiRef<CellValueMask> GetRow(int row) => _cells.GetRowReference(row);
    public MultiRef<CellValueMask> GetColumn(int columns) => _cells.GetColumnReference(columns);
    public MultiRef<CellValueMask> GetRange(Range rows, Range columns) => _cells.GetRectangle(rows, columns);
    public MultiRef<CellValueMask> Unbox(MultiRefBox<CellValueMask> box) => _cells.Unbox(box);

    public Cells MoveToImmutable()
    {
        return new Cells(_cells.MoveToImmutable());
    }

    public void SetSingle(int row, int column, CellValue value)
    {
        _cells[row, column] = value.AsMask();
    }

    public CellValue GetSingle(int row, int column) => _cells[row, column].GetSingle();

    public static CellsBuilder CreateFilled(GameStructure gameStructure)
    {
        ImmutableArray2.Builder<CellValueMask> cells = ImmutableArray2.CreateBuilder<CellValueMask>(gameStructure.Rows, gameStructure.Columns);
        cells.Fill(CellValueMask.All(gameStructure.Digits));
        return new CellsBuilder(cells);
    }
}