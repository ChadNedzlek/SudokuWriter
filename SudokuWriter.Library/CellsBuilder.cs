namespace SudokuWriter.Library;

public readonly struct CellsBuilder
{
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
    
    public ref ushort this[int row, int col] => ref _cells[row, col];

    public Cells MoveToImmutable() => new Cells(_cells.MoveToImmutable());
}