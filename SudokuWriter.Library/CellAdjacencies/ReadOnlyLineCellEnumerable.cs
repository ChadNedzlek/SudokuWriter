using System.Collections;
using System.Collections.Generic;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Library.CellAdjacencies;

public class ReadOnlyLineCellEnumerable : IEnumerable<ReadOnlyLineCellAdjacency>
{
    private readonly Cells _cells;
    private readonly ILineRule _rule;

    public ReadOnlyLineCellEnumerable(Cells cells, ILineRule rule)
    {
        _cells = cells;
        _rule = rule;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator<ReadOnlyLineCellAdjacency> IEnumerable<ReadOnlyLineCellAdjacency>.GetEnumerator()
    {
        return GetEnumerator();
    }

    public ReadOnlyLineCellEnumerator GetEnumerator()
    {
        return new ReadOnlyLineCellEnumerator(_cells, _rule);
    }
}