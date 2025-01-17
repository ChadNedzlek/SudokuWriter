using SudokuWriter.Library.Rules;

namespace SudokuWriter.Library.CellAdjacencies;

public readonly struct LineCellEnumerable
{
    private readonly CellsBuilder _cells;
    private readonly ILineRule _rule;

    public LineCellEnumerable(CellsBuilder cells, ILineRule rule)
    {
        _cells = cells;
        _rule = rule;
    }

    public LineCellEnumerator GetEnumerator() => new(_cells, _rule);
}