using System;
using System.Collections.Immutable;
using System.Numerics;
using System.Text.Json.Nodes;

namespace SudokuWriter.Library.Rules;

[GameRule("diff-line")]
public class DifferenceAtLeastLineRule : DifferenceAtLeastLineRuleBase<DifferenceAtLeastLineRule>, ILineRule<DifferenceAtLeastLineRule>
{
    protected DifferenceAtLeastLineRule(ImmutableList<LineRuleSegment> segments, ushort minDifference) : base(segments, minDifference)
    {
    }

    public static IGameRule Create(ImmutableList<LineRuleSegment> parts, JsonObject jsonObject) => new DifferenceAtLeastLineRule(parts, jsonObject["diff"].GetValue<ushort>());
}

public abstract class DifferenceAtLeastLineRuleBase<T> : LineRule<T>
    where T : ILineRule<T>
{
    public ushort MinDifference { get; }
    
    protected DifferenceAtLeastLineRuleBase(ImmutableList<LineRuleSegment> segments, ushort minDifference) : base(segments)
    {
        MinDifference = minDifference;
    }

    public override GameResult Evaluate(GameState state)
    {
        foreach (LineRuleSegment segment in Segments)
        {
            ushort startMask = state.Cells.GetMask(segment.Start.Row, segment.Start.Col);
            ushort endMask = state.Cells.GetMask(segment.End.Row, segment.End.Col);

            ushort endAllowedMask = GetAllowedMask(startMask);
            ushort startAllowedMask = GetAllowedMask(endMask);

            int endOverlap = endMask & endAllowedMask;
            if (endOverlap == 0)
                return GameResult.Unsolvable;
            
            int startOverlap = startMask & startAllowedMask;
            if (startOverlap == 0)
                return GameResult.Unsolvable;
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
        foreach (LineRuleSegment segment in Segments)
        {
            ref ushort startMask = ref cellBuilder[segment.Start.Row, segment.Start.Col];
            ref ushort endMask = ref cellBuilder[segment.End.Row, segment.End.Col];

            ushort endAllowedMask = GetAllowedMask(startMask);
            ushort startAllowedMask = GetAllowedMask(endMask);

            ushort nextEnd = (ushort)(endMask & endAllowedMask);
            ushort nextStart = (ushort)(startMask & startAllowedMask);
            if (nextEnd != endMask || nextStart != startMask)
            {
                modified = true;
            }

            startMask = nextStart;
            endMask = nextEnd;
        }

        return modified ? state.WithCells(cellBuilder.MoveToImmutable()) : null;
    }
}