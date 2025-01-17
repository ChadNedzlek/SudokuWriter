namespace SudokuWriter.Library.CellAdjacencies;

public readonly ref struct LineCellAdjacency
{
    public readonly ushort Cell;
    public readonly GridCoord CellCoord;
    public readonly MultiRef<ushort> AdjacentCells;

    public LineCellAdjacency(ushort cell, GridCoord cellCoord, MultiRef<ushort> adjacentCells)
    {
        Cell = cell;
        CellCoord = cellCoord;
        AdjacentCells = adjacentCells;
    }
}