using System;
using System.Collections.Immutable;
using System.Linq;

namespace VaettirNet.SudokuWriter.Library.Rules;

public abstract class DirectedLineRule<T> : LineRule<T>
    where T : ILineRule<T>
{
    private readonly Lazy<ImmutableArray<GridCoord>> _roots;
    private readonly Lazy<ImmutableArray<GridCoord>> _leaves;

    protected DirectedLineRule(ImmutableArray<BranchingRuleLine> lines) : base(lines)
    {
        _roots = new(FindRoots);
        _leaves = new(FindLeaves);
    }

    public ImmutableArray<GridCoord> FindRoots()
    {
        var b = ImmutableArray.CreateBuilder<GridCoord>();
        foreach (BranchingRuleLine line in Lines)
        {
            foreach (LineRuleSegment branch in line.Branches)
            {
                GridCoord root = branch.Cells[0];
                if (!line.Branches.Any(b => b.Cells[1..].Any(c => c == root))) b.Add(root);
            }
        }

        return b.ToImmutable();
    }

    public ImmutableArray<GridCoord> FindLeaves()
    {
        var b = ImmutableArray.CreateBuilder<GridCoord>();
        foreach (BranchingRuleLine line in Lines)
        {
            foreach (LineRuleSegment branch in line.Branches)
            {
                GridCoord leaf = branch.Cells[^1];
                if (!line.Branches.Any(b => b.Cells[..^1].Any(c => c == leaf))) b.Add(leaf);
            }
        }

        return b.ToImmutable();
    }

    public int GetNext(GridCoord start, Span<GridCoord> next)
    {
        int c = 0;
        foreach (BranchingRuleLine line in Lines)
        {
            foreach (LineRuleSegment branch in line.Branches)
            {
                for (int s = 0; s < branch.Cells.Length - 1; s++)
                {
                    if (branch.Cells[s] == start)
                    {
                        next[c++] = branch.Cells[s + 1];
                    }
                }
            }
        }

        return c;
    }
    public int GetPrevious(GridCoord start, Span<GridCoord> next)
    {
        int c = 0;
        foreach (BranchingRuleLine line in Lines)
        {
            foreach (LineRuleSegment branch in line.Branches)
            {
                for (int s = 1; s < branch.Cells.Length; s++)
                {
                    if (branch.Cells[s] == start)
                    {
                        next[c++] = branch.Cells[s - 1];
                    }
                }
            }
        }

        return c;
    }

    public ImmutableArray<GridCoord> GetRoots() => _roots.Value;
    public ImmutableArray<GridCoord> GetLeaves() => _leaves.Value;
}