using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Library.CellAdjacencies;

public struct LineCellEnumerator
{
    private readonly CellsBuilder _cells;
    private readonly ILineRule _rule;
    private bool _ended = false;
    private int _lineIndex = -1;
    private int _segmentIndex;
    private int _cellIndex;
    private BranchingRuleLine _currentLine;
    private LineRuleSegment _currentBranch;

    public LineCellEnumerator(CellsBuilder cells, ILineRule rule)
    {
        _cells = cells;
        _rule = rule;
    }

    public bool MoveNext()
    {
        if (_ended) return false;
        if (_lineIndex == -1) return MoveToNextLine();

        return ++_cellIndex < _currentBranch.Cells.Length || MoveToNextSegment();
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

            _currentLine = _rule.Lines[_lineIndex];

            if (_currentLine.Branches.Length > 0)
            {
                _segmentIndex = -1;
                if (MoveToNextSegment()) return true;
            }
        }

        _ended = true;
        return false;
    }

    public LineCellAdjacency Current
    {
        get
        {
            GridCoord coord = _currentBranch.Cells[_cellIndex];
            MultiRef<CellValueMask> refs = _cells.GetEmptyReferences();


            AddNeighbors(ref refs, _currentBranch, _cellIndex);

            if (_cellIndex != 0 && _cellIndex != _currentBranch.Cells.Length - 1)
            {
                foreach (LineRuleSegment branch in _currentLine.Branches)
                {
                    if (branch.Cells[0] == coord) refs.Include(ref _cells[branch.Cells[1]]);

                    if (branch.Cells[^1] == coord) refs.Include(ref _cells[branch.Cells[^2]]);
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
                        if (cell == coord) AddNeighbors(ref refs, branch, iCell);
                    }
                }
            }

            return new LineCellAdjacency(_cells[coord], coord, refs);
        }
    }

    private void AddNeighbors(ref MultiRef<CellValueMask> refs, LineRuleSegment branch, int iCell)
    {
        if (iCell > 0)
            refs.Include(ref _cells[branch.Cells[iCell - 1]]);
        if (iCell < branch.Cells.Length - 1)
            refs.Include(ref _cells[branch.Cells[iCell + 1]]);
    }
}