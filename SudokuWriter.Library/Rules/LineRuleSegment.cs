using System.Collections.Immutable;

namespace VaettirNet.SudokuWriter.Library.Rules;

public readonly record struct LineRuleSegment(params ImmutableArray<GridCoord> Cells);

public readonly record struct BranchingRuleLine(params ImmutableArray<LineRuleSegment> Branches);