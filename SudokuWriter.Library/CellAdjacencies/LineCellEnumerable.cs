using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Library.CellAdjacencies;

public readonly struct LineCellEnumerable
{
    private readonly CellsBuilder _cells;
    private readonly ILineRule _rule;

    public LineCellEnumerable(CellsBuilder cells, ILineRule rule)
    {
        _cells = cells;
        _rule = rule;
    }

    public LineCellEnumerator GetEnumerator()
    {
        return new LineCellEnumerator(_cells, _rule);
    }
}