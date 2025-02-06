using System.Collections;
using System.Collections.Generic;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Library.CellAdjacencies;

public class ReadOnlyLineCellEnumerator : IEnumerator<ReadOnlyLineCellAdjacency>
{
    private readonly Cells _cells;
    private readonly ILineRule _rule;
    private int _cellIndex;
    private LineRuleSegment _currentBranch;
    private BranchingRuleLine _currentLine;
    private bool _ended;
    private int _lineIndex = -1;
    private int _segmentIndex;

    public ReadOnlyLineCellEnumerator(Cells cells, ILineRule rule)
    {
        _cells = cells;
        _rule = rule;
    }

    public ReadOnlyLineCellAdjacency Current
    {
        get
        {
            GridCoord coord = _currentBranch.Cells[_cellIndex];
            List<CellValueMask> adjacentCells = [];


            AddNeighbors(adjacentCells, _currentBranch, _cellIndex);

            if (_cellIndex != 0 && _cellIndex != _currentBranch.Cells.Length - 1)
            {
                foreach (LineRuleSegment branch in _currentLine.Branches)
                {
                    if (branch.Cells[0] == coord) adjacentCells.Add(_cells[branch.Cells[1]]);

                    if (branch.Cells[^1] == coord) adjacentCells.Add(_cells[branch.Cells[^2]]);
                }
            }
            else
            {
                for (int iBranch = 0; iBranch < _currentLine.Branches.Length; iBranch++)
                {
                    if (iBranch == _segmentIndex) continue;
                    LineRuleSegment branch = _currentLine.Branches[iBranch];
                    for (int iCell = 0; iCell < branch.Cells.Length; iCell++)
                    {
                        GridCoord cell = branch.Cells[iCell];
                        if (cell == coord) AddNeighbors(adjacentCells, branch, iCell);
                    }
                }
            }

            return new ReadOnlyLineCellAdjacency(_cells[coord], coord, adjacentCells);
        }
    }

    public void Reset()
    {
        _ended = false;
        _lineIndex = -1;
    }

    public bool MoveNext()
    {
        if (_ended) return false;
        if (_lineIndex == -1) return MoveToNextLine();

        return ++_cellIndex < _currentBranch.Cells.Length || MoveToNextSegment();
    }

    ReadOnlyLineCellAdjacency IEnumerator<ReadOnlyLineCellAdjacency>.Current => Current;

    object IEnumerator.Current => Current;

    public void Dispose()
    {
    }

    private bool MoveToNextSegment()
    {
        while (_segmentIndex < _currentLine.Branches.Length - 1)
        {
            _segmentIndex++;

            _currentBranch = _currentLine.Branches[_segmentIndex];

            if (_currentBranch.Cells.Length > 0)
            {
                _cellIndex = 0;
                return true;
            }
        }

        return MoveToNextLine();
    }

    private bool MoveToNextLine()
    {
        while (_lineIndex < _rule.Lines.Length - 1)
        {
            _lineIndex++;

            _currentLine = _rule.Lines[_segmentIndex];

            if (_currentLine.Branches.Length > 0)
            {
                _segmentIndex = -1;
                if (MoveToNextSegment()) return true;
            }
        }

        _ended = true;
        return false;
    }

    private void AddNeighbors(in List<CellValueMask> adjacentCells, LineRuleSegment branch, int iCell)
    {
        if (iCell > 0)
            adjacentCells.Add(_cells[branch.Cells[iCell - 1]]);
        if (iCell < branch.Cells.Length - 1)
            adjacentCells.Add(_cells[branch.Cells[iCell + 1]]);
    }
}