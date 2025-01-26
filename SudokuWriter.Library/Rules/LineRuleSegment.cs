using System.Collections.Immutable;

namespace VaettirNet.SudokuWriter.Library.Rules;

public readonly record struct LineRuleSegment(ImmutableArray<GridCoord> Cells);

public readonly record struct BranchingRuleLine(ImmutableArray<LineRuleSegment> Branches)
{
}