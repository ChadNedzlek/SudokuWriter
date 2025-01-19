using SudokuWriter.Library;

namespace SudokuWriter.Gui.UiRules;

public readonly record struct CellLocation(int Row, int Col, int RowSide = 0, int ColSide = 0)
{
    public CellLocation Center => this with { ColSide = 0, RowSide = 0 };
    public bool IsCenter => RowSide == 0 && ColSide == 0;

    public override string ToString() => $"({Row} [{RowSide}], {Col} [{ColSide}])";

    public GridCoord ToCoord() => new((ushort)Row, (ushort)Col);
}