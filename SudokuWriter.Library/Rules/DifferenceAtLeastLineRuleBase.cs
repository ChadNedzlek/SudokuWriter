using System;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;
using SudokuWriter.Library.CellAdjacencies;

namespace SudokuWriter.Library.Rules;

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
            ushort cellMask = adjacency.Cell;
            ushort allowedMask = GetAllowedMask(cellMask);
            if (adjacency.AdjacentCells.Any(c => (c & allowedMask) == 0))
            {
                return GameResult.Unsolvable;
            }
        }

        return GameResult.Solved;
    }

    private ushort GetAllowedMask(ushort otherMask)
    {
        ushort endAllowedMask = 0;
        ushort shift = (ushort)(otherMask >> MinDifference);
        while (shift != 0)
        {
            endAllowedMask |= shift;
            shift >>= 1;
        }
        shift = (ushort)(otherMask << MinDifference);
        while (shift != 0)
        {
            endAllowedMask |= shift;
            shift <<= 1;
        }

        return endAllowedMask;
    }

    public override GameState? TryReduce(GameState state)
    {
        CellsBuilder cellBuilder = state.Cells.ToBuilder();
        bool modified = false;
        foreach (LineCellAdjacency adjacency in GetLineAdjacencies(cellBuilder))
        {
            ushort allowedMask = GetAllowedMask(adjacency.Cell);

            modified = adjacency.AdjacentCells.Aggregate(modified,
                (bool mod, ref ushort cell) => RuleHelpers.TryUpdate(ref cell, (ushort)(cell & allowedMask)) | mod
            );
        }

        return modified ? state.WithCells(cellBuilder.MoveToImmutable()) : null;
    }
}