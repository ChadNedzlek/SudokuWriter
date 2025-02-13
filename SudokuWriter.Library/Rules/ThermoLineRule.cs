using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace VaettirNet.SudokuWriter.Library.Rules;

[GameRule("thermo")]
public class ThermoLineRule : DirectedLineRule<ThermoLineRule>, ILineRule<ThermoLineRule>
{
    public ThermoLineRule(ImmutableArray<BranchingRuleLine> lines) : base(lines)
    {
    }

    public static ThermoLineRule Create(ImmutableArray<BranchingRuleLine> parts, JsonObject jsonObject) => new ThermoLineRule(parts);

    public override GameResult Evaluate(GameState state)
    {
        ImmutableArray<GridCoord> roots = GetRoots();
        var coordQueue = new CircularSpanQueue<GridCoord>(stackalloc GridCoord[40]);
        var valueQueue = new CircularSpanQueue<CellValueMask>(stackalloc CellValueMask[40]);
        foreach (GridCoord r in roots)
        {
            coordQueue.Enqueue(r);
            valueQueue.Enqueue(state.Cells[r]);
        }

        Span<GridCoord> next = stackalloc GridCoord[8];
        while (coordQueue.TryDequeue(out GridCoord c))
        {
            valueQueue.TryDequeue(out CellValueMask v);
            int cNext = GetNext(c, next);
            for (int i = 0; i < cNext; i++)
            {
                GridCoord crdNext = next[i];
                CellValueMask vNext = state.Cells[crdNext];
                if (vNext.GetMaxValue() <= v.GetMinValue())
                    return GameResult.Unsolvable;
                coordQueue.Enqueue(crdNext);
                valueQueue.Enqueue(vNext);
            }
        }

        return GameResult.Solved;
    }

    public override GameState? TryReduce(GameState state, ISimplificationChain chain)
    {
        var cells = state.Cells.ToBuilder();
        bool reduced = false;
        ImmutableArray<GridCoord> roots = GetRoots();
        var coordQueue = new CircularSpanQueue<GridCoord>(stackalloc GridCoord[20]);
        var valueQueue = new CircularSpanQueue<CellValueMask>(stackalloc CellValueMask[20]);
        foreach (GridCoord r in roots)
        {
            coordQueue.Enqueue(r);
            valueQueue.Enqueue(state.Cells[r]);
        }

        Span<GridCoord> next = stackalloc GridCoord[8];
        while (coordQueue.TryDequeue(out GridCoord c))
        {
            valueQueue.TryDequeue(out CellValueMask v);
            int cNext = GetNext(c, next);
            for (int i = 0; i < cNext; i++)
            {
                GridCoord crdNext = next[i];
                ref CellValueMask vNext = ref cells[crdNext];
                CellValueMask minV = CellValueMask.All(state.Digits) << v.GetMinValue().NumericValue;
                reduced |= RuleHelpers.TryMask(ref vNext,  minV);
                coordQueue.Enqueue(crdNext);
                valueQueue.Enqueue(vNext);
            }
        }
        
        ImmutableArray<GridCoord> tails = GetLeaves();
        foreach (GridCoord r in tails)
        {
            coordQueue.Enqueue(r);
            valueQueue.Enqueue(state.Cells[r]);
        }
        while (coordQueue.TryDequeue(out GridCoord c))
        {
            valueQueue.TryDequeue(out CellValueMask v);
            int cNext = GetPrevious(c, next);
            for (int i = 0; i < cNext; i++)
            {
                GridCoord crdNext = next[i];
                ref CellValueMask vNext = ref cells[crdNext];
                CellValueMask maxV = CellValueMask.All(v.GetMaxValue().NumericValue - 1);
                reduced |= RuleHelpers.TryMask(ref vNext,  maxV);
                coordQueue.Enqueue(crdNext);
                valueQueue.Enqueue(vNext);
            }
        }

        return reduced ? state.WithCells(cells.MoveToImmutable()) : null;
    }

    public override IEnumerable<MutexGroup> GetMutualExclusionGroups(GameState state, ISimplificationTracker tracker)
    {
        List<MutexGroup> groups = new();
        foreach (BranchingRuleLine line in Lines)
        {
            ReadOnlyMultiRef<CellValueMask> grp = state.Cells.GetEmptyReferences();
            foreach (LineRuleSegment branch in line.Branches)
            foreach(GridCoord cell in branch.Cells)
                grp.Include(in state.Cells[cell]);
            groups.Add(new (grp.Box(), tracker.Record($"Thermo {line.Branches[0].Cells[0]}")));
        }

        return groups;
    }

    public override IEnumerable<DigitFence> GetFencedDigits(GameState state, ISimplificationTracker tracker) => [];
}