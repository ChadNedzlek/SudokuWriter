namespace VaettirNet.SudokuWriter.Library.CellAdjacencies;

public readonly ref struct LineCellAdjacency
{
    public readonly CellValueMask Cell;
    public readonly GridCoord CellCoord;
    public readonly MultiRef<CellValueMask> AdjacentCells;

    public LineCellAdjacency(CellValueMask cell, GridCoord cellCoord, MultiRef<CellValueMask> adjacentCells)
    {
        Cell = cell;
        CellCoord = cellCoord;
        AdjacentCells = adjacentCells;
    }
}