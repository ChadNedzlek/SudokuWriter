using System.Collections.Generic;

namespace SudokuWriter.Library.CellAdjacencies;

public readonly record struct ReadOnlyLineCellAdjacency(ushort Cell, GridCoord Coord, List<ushort> AdjacentCells);