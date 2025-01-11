namespace SudokuWriter.Library;

public readonly struct GameState
{
    public static GameState Default { get; } = new(new Cells(new ImmutableArray2<ushort>(9, 9))); 
    
    public GameState(Cells cells, int boxRows = 3, int boxColumns = 3, int digits = 9)
    {
        Cells = cells;
        BoxRows = boxRows;
        BoxColumns = boxColumns;
        Digits = digits;
    }

    public Cells Cells { get; }
    public int BoxRows { get; }
    public int BoxColumns { get; }
    public int Digits { get; }

    public GameState WithCells(Cells cells) => new(cells, BoxRows, BoxColumns, Digits);
}