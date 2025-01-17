using System.Collections;
using System.Collections.Generic;
using SudokuWriter.Library.Rules;

namespace SudokuWriter.Library.CellAdjacencies;

public class ReadOnlyLineCellEnumerable : IEnumerable<ReadOnlyLineCellAdjacency>
{
    private readonly Cells _cells;
    private readonly ILineRule _rule;

    public ReadOnlyLineCellEnumerable(Cells cells, ILineRule rule)
    {
        _cells = cells;
        _rule = rule;
    }

    public ReadOnlyLineCellEnumerator GetEnumerator() => new(_cells, _rule);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IEnumerator<ReadOnlyLineCellAdjacency> IEnumerable<ReadOnlyLineCellAdjacency>.GetEnumerator() => GetEnumerator();
}