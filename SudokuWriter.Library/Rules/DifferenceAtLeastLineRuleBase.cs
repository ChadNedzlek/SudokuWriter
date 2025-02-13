using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Nodes;
using VaettirNet.SudokuWriter.Library.CellAdjacencies;

namespace VaettirNet.SudokuWriter.Library.Rules;

[GameRule("diff-line")]
public class DifferenceAtLeastLineRule : DifferenceAtLeastLineRuleBase<DifferenceAtLeastLineRule>, ILineRule<DifferenceAtLeastLineRule>
{
    public DifferenceAtLeastLineRule(ImmutableArray<BranchingRuleLine> lines, ushort minDifference) : base(lines, minDifference)
    {
    }
    
    public DifferenceAtLeastLineRule(ImmutableArray<LineRuleSegment> segments, ushort minDifference) : this([new BranchingRuleLine(segments)], minDifference)
    {
    }
    
    public DifferenceAtLeastLineRule(ImmutableArray<GridCoord> cells, ushort minDifference) : this([new LineRuleSegment(cells)], minDifference)
    {
    }

    public static IGameRule Create(ImmutableArray<BranchingRuleLine> parts, JsonObject jsonObject) => new DifferenceAtLeastLineRule(parts, jsonObject["diff"].GetValue<ushort>());

    public override void SaveToJsonObject(JsonObject obj)
    {
        obj["diff"] = MinDifference;
    }
}

public abstract class DifferenceAtLeastLineRuleBase<T> : LineRule<T>
    where T : ILineRule<T>
{
    public ushort MinDifference { get; }
    
    protected DifferenceAtLeastLineRuleBase(ImmutableArray<BranchingRuleLine> lines, ushort minDifference) : base(lines)
    {
        MinDifference = minDifference;
    }

    public override GameResult Evaluate(GameState state)
    {
        foreach (ReadOnlyLineCellAdjacency adjacency in GetLineAdjacencies(state.Cells))
        {
            CellValueMask cellMask = adjacency.Cell;
            CellValueMask allowedMask = GetAllowedMask(cellMask);
            if (adjacency.AdjacentCells.Any(c => (c & allowedMask) == CellValueMask.None))
            {
                return GameResult.Unsolvable;
            }
        }

        return GameResult.Solved;
    }

    private CellValueMask GetAllowedMask(CellValueMask otherMask)
    {
        CellValueMask endAllowedMask = CellValueMask.None;
        CellValueMask shift = otherMask >> MinDifference;
        while (shift != CellValueMask.None)
        {
            endAllowedMask |= shift;
            shift >>= 1;
        }
        shift = otherMask << MinDifference;
        while (shift != CellValueMask.None)
        {
            endAllowedMask |= shift;
            shift <<= 1;
        }

        return endAllowedMask;
    }

    public override GameState? TryReduce(GameState state, ISimplificationChain chain)
    {
        CellsBuilder cellBuilder = state.Cells.ToBuilder();
        bool modified = false;
        foreach (LineCellAdjacency adjacency in GetLineAdjacencies(cellBuilder))
        {
            CellValueMask allowedMask = GetAllowedMask(adjacency.Cell);

            modified = adjacency.AdjacentCells.Aggregate(modified, RuleHelpers.TryMask, allowedMask);
        }

        return modified ? state.WithCells(cellBuilder.MoveToImmutable()) : null;
    }

    public override IEnumerable<MutexGroup> GetMutualExclusionGroups(GameState state, ISimplificationTracker tracker) => [];
    public override IEnumerable<DigitFence> GetFencedDigits(GameState state, ISimplificationTracker tracker) => [];
}