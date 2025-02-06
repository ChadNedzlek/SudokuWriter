using System.Collections.Generic;

namespace VaettirNet.SudokuWriter.Library.CellAdjacencies;

public readonly record struct ReadOnlyLineCellAdjacency(CellValueMask Cell, GridCoord Coord, List<CellValueMask> AdjacentCells);