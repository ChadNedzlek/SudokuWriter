using System.Collections;
using System.Collections.Generic;
using SudokuWriter.Library.Rules;

namespace SudokuWriter.Library.CellAdjacencies;

public class ReadOnlyLineCellEnumerator : IEnumerator<ReadOnlyLineCellAdjacency>
{
    private readonly Cells _cells;
    private readonly ILineRule _rule;
    private bool _ended = false;
    private int _lineIndex = -1;
    private int _segmentIndex;
    private int _cellIndex;
    private BranchingRuleLine _currentLine;
    private LineRuleSegment _currentBranch;

    public ReadOnlyLineCellEnumerator(Cells cells, ILineRule rule)
    {
        _cells = cells;
        _rule = rule;
    }

    public void Reset()
    {
        _ended = false;
        _lineIndex = -1;
    }

    public bool MoveNext()
    {
        if (_ended) return false;
        if (_lineIndex == -1)
        {
            return MoveToNextLine();
        }
        
        return ++_cellIndex < _currentBranch.Cells.Length || MoveToNextSegment();
    }

    private bool MoveToNextSegment()
    {
        while (_segmentIndex < _rule.Lines.Length)
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
        while (_lineIndex < _rule.Lines.Length)
        {
            _lineIndex++;

            _currentLine = _rule.Lines[_segmentIndex];

            if (_currentLine.Branches.Length > 0)
            {
                _segmentIndex = -1;
                if (MoveToNextSegment())
                {
                    return true;
                }
            }
        }

        _ended = true;
        return false;
    }

    public ReadOnlyLineCellAdjacency Current
    {
        get
        {
            GridCoord coord = _currentBranch.Cells[_cellIndex];
            List<ushort> adjacentCells = [];

            
            AddNeighbors(adjacentCells, _currentBranch, _cellIndex);

            if (_cellIndex != 0 && _cellIndex != _currentBranch.Cells.Length - 1)
            {
                foreach (var branch in _currentLine.Branches)
                {
                    if (branch.Cells[0] == coord)
                    {
                        adjacentCells.Add(_cells[branch.Cells[1]]);
                    }

                    if (branch.Cells[^1] == coord)
                    {
                        adjacentCells.Add(_cells[branch.Cells[^2]]);
                    }
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
                        if (cell == coord)
                        {
                            AddNeighbors(adjacentCells, branch, iCell);
                        }
                    }
                }
            }

            return new ReadOnlyLineCellAdjacency(_cells[coord], coord, adjacentCells);
        }
    }

    private void AddNeighbors(in List<ushort> adjacentCells, LineRuleSegment branch, int iCell)
    {
        if (iCell > 0)
            adjacentCells.Add(_cells[branch.Cells[iCell - 1]]);
        if (iCell < branch.Cells.Length - 1)
            adjacentCells.Add(_cells[branch.Cells[iCell + 1]]);
    }

    ReadOnlyLineCellAdjacency IEnumerator<ReadOnlyLineCellAdjacency>.Current => Current;

    object IEnumerator.Current => Current;

    public void Dispose()
    {
    }
}