using System.Collections.Immutable;

namespace SudokuWriter.Library.CellAdjacencies;

public readonly struct ReadOnlyCellGroup(ushort RequiredDigits, ImmutableArray<GridCoord> Cells);