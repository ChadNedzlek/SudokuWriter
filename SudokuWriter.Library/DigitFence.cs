using System.Collections.Immutable;

namespace VaettirNet.SudokuWriter.Library;

public readonly record struct DigitFence(CellValue Digit, MultiRefBox<CellValueMask> Cells, SimplificationRecord SimplificationRecord);