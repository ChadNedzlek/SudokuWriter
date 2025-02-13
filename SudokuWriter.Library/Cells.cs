using System;
using System.Runtime.InteropServices;

namespace VaettirNet.SudokuWriter.Library;

public readonly struct Cells
{
    private readonly ImmutableArray2<CellValueMask> _cells;

    public Cells(ImmutableArray2<CellValueMask> cells)
    {
        _cells = cells;
    }

    public int Rows => _cells.Rows;
    public int Columns => _cells.Columns;

    public bool this[int row, int col, CellValue digit] => _cells[row, col].Contains(digit);
    public ref readonly CellValueMask this[int row, int col] => ref _cells[row, col];
    public ref readonly CellValueMask this[GridCoord coord] => ref _cells[coord.Row, coord.Col];

    public CellValue GetSingle(int row, int col) => _cells[row, col].GetSingle();
    
    public ReadOnlyMultiRef<CellValueMask> GetEmptyReferences() => _cells.GetEmptyReference();
    public ReadOnlyMultiRef<CellValueMask> GetRow(int row) => _cells.GetRowReference(row);
    public ReadOnlyMultiRef<CellValueMask> GetColumn(int columns) => _cells.GetColumnReference(columns);
    public ReadOnlyMultiRef<CellValueMask> GetRange(Range rows, Range columns) => _cells.GetRectangle(rows, columns);
    public ReadOnlyMultiRef<CellValueMask> Unbox(MultiRefBox<CellValueMask> box) => _cells.Unbox(box);

    public CellsBuilder ToBuilder()
    {
        return new CellsBuilder(_cells.ToBuilder());
    }

    public static Cells CreateFilled(int rows = 9, int columns = 9, int digits = 9)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(digits);
        ImmutableArray2.Builder<CellValueMask> array = ImmutableArray2.CreateBuilder<CellValueMask>(rows, columns);
        array.Fill(CellValueMask.All(digits));
        return new Cells(array.MoveToImmutable());
    }

    public Cells SetCell(int row, int column, CellValue digit)
    {
        return new Cells(_cells.SetItem(row, column, digit.AsMask()));
    }

    public Cells SetCell(int row, int column, ushort digit) => SetCell(row, column, new CellValue(digit));

    public Cells FillEmpty(GameStructure structure)
    {
        ImmutableArray2.Builder<CellValueMask> b = _cells.ToBuilder();
        for (int r = 0; r < b.Rows; r++)
        for (int c = 0; c < b.Columns; c++)
        {
            ref CellValueMask cell = ref b[r, c];
            if (cell == CellValueMask.None) cell = CellValueMask.All(structure.Digits);
        }

        return new Cells(b.MoveToImmutable());
    }

    public ulong GetCellHash()
    {
        return _cells.GetHash64();
    }

    public static Cells CreateFilled(GameStructure structure)
    {
        return CreateFilled(structure.Rows, structure.Columns, structure.Digits);
    }

    public static Cells FromMasks(CellValueMask[,] masks)
    {
        var cells = new CellValueMask[masks.Length];
        unsafe
        {
            ReadOnlySpan<CellValueMask> span = MemoryMarshal.Cast<byte, CellValueMask>(
                MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(masks), sizeof(CellValueMask) * masks.Length)
            );
            span.CopyTo(cells);
        }

        return new Cells(new ImmutableArray2<CellValueMask>(cells, masks.GetLength(1)));
    }

    public static Cells FromDigits(ushort[,] digits)
    {
        CellValueMask[] cells = new CellValueMask[digits.Length];
        ReadOnlySpan<CellValue> span = MemoryMarshal.Cast<byte, CellValue>(MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(digits), sizeof(ushort) * digits.Length));
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = span[i].AsMask();
        }
        return new Cells(new ImmutableArray2<CellValueMask>(cells, digits.GetLength(1)));
    }

    public bool Any(Func<CellValueMask, bool> selector) => _cells.Any(selector);
}