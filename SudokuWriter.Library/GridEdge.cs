namespace VaettirNet.SudokuWriter.Library;

public readonly record struct GridEdge(ushort EdgeRow, ushort EdgeCol)
{
    public (GridCoord, GridCoord) GetSides()
    {
        ushort cellRow = unchecked((ushort)(EdgeRow / 2));
        if (EdgeRow % 2 == 0)
        {
            return ((cellRow, EdgeCol), (cellRow, unchecked((ushort)(EdgeCol + 1))));
        }

        return ((cellRow, EdgeCol), (unchecked((ushort)(cellRow + 1)), EdgeCol));
    }

    public GridCoord? GetOther(GridCoord cell)
    {
        ushort cellRow = unchecked((ushort)(EdgeRow / 2));
        if (EdgeRow % 2 == 0)
        {
            if (cellRow != cell.Row) return null;
            if (EdgeCol == cell.Col) return (cellRow, unchecked((ushort)(EdgeCol + 1)));
            if (EdgeCol + 1 == cell.Col) return (cellRow, EdgeCol);
        }

        if (EdgeCol != cell.Col) return null;
        if (EdgeRow == cell.Row) return (unchecked((ushort)(cellRow + 1)), EdgeCol);
        if (EdgeRow == cellRow + 1) return (cellRow, EdgeCol);

        return null;
    }

    public static GridEdge RightOf(GridCoord coord) => new((ushort)(coord.Row * 2), coord.Col);
    public static GridEdge BottomOf(GridCoord coord) => new((ushort)(coord.Row * 2 + 1), coord.Col);

    public static implicit operator GridEdge((ushort row, ushort col) e) => new(e.row, e.col);
}