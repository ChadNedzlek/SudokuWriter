using System.Collections.Generic;
using System.Collections.Immutable;

namespace VaettirNet.SudokuWriter.Library.Rules;

public abstract class TriGroupLineRule<T> : TriLineRule<T>
    where T : ILineRule<T>
{
    protected TriGroupLineRule(ImmutableArray<BranchingRuleLine> lines) : base(lines)
    {
    }

    protected abstract CellValueMask ReduceToGroup(CellValueMask input);
    protected abstract CellValueMask ReducingGroupMask(CellValueMask mask);
    protected abstract CellValueMask InputGroupMask { get; }

    protected override GameResult EvaluateGroup(in ReadOnlyMultiRef<CellValueMask> a, in ReadOnlyMultiRef<CellValueMask> b, in ReadOnlyMultiRef<CellValueMask> c)
    {
        CellValueMask groupMask = InputGroupMask;
        var anyA = a.Aggregate(CellValueMask.None, (m, v) => m | ReduceToGroup(v) & groupMask);
        var anyB = b.Aggregate(CellValueMask.None, (m, v) => m | ReduceToGroup(v) & groupMask);
        var anyC = c.Aggregate(CellValueMask.None, (m, v) => m | ReduceToGroup(v) & groupMask);
        
        bool cHasCells = !c.IsEmpty;
        if (cHasCells)
        {
            if ((anyA | anyB | anyC) != groupMask) return GameResult.Unsolvable;
        }
        else
        {
            if ((anyA | anyB).Count < 2) return GameResult.Unsolvable;
        }

        var onlyA = a.Aggregate(groupMask, (m, v) => m & ReduceToGroup(v));
        var onlyB = b.Aggregate(groupMask, (m, v) => m & ReduceToGroup(v));
        var onlyC = c.Aggregate(groupMask, (m, v) => m & ReduceToGroup(v));

        if (onlyA == CellValueMask.None || onlyB == CellValueMask.None || (onlyC == CellValueMask.None && cHasCells))
            return GameResult.Unsolvable;
        
        return onlyA.Count == 1 && onlyB.Count == 1 && (onlyC.Count == 1 || !cHasCells) ? GameResult.Solved : GameResult.Unknown;
    }

    protected override bool ReduceGroups(ref MultiRef<CellValueMask> a, ref MultiRef<CellValueMask> b, ref MultiRef<CellValueMask> c)
    {
        CellValueMask groupMask = InputGroupMask;
        bool reduced = false;
        
        var onlyA = a.Aggregate(groupMask, (CellValueMask m, ref CellValueMask v) => m & ReduceToGroup(v));
        var onlyB = b.Aggregate(groupMask, (CellValueMask m, ref CellValueMask v) => m & ReduceToGroup(v));
        var onlyC = c.Aggregate(groupMask, (CellValueMask m, ref CellValueMask v) => m & ReduceToGroup(v));

        reduced |= MaskOtherCells(onlyA, ref b, ref c);
        reduced |= MaskOtherCells(onlyB, ref a, ref c);
        reduced |= MaskOtherCells(onlyC, ref a, ref b);
        reduced |= SelfMask(onlyA, ref a);
        reduced |= SelfMask(onlyB, ref b);
        reduced |= SelfMask(onlyC, ref c);

        return reduced;

        bool SelfMask(CellValueMask src, ref MultiRef<CellValueMask> a)
        {
            return a.Aggregate(false, RuleHelpers.TryMask, ReducingGroupMask(src));
        }

        bool MaskOtherCells(CellValueMask src, ref MultiRef<CellValueMask> b, ref MultiRef<CellValueMask> c)
        {
            if (src.Count != 1)
            {
                return false;
            }

            bool reduced = false;
            CellValueMask groupMask = InputGroupMask;
            var aMask = ReducingGroupMask(groupMask & ~src);
            reduced |= b.Aggregate(false, RuleHelpers.TryMask, aMask);
            reduced |= c.Aggregate(false, RuleHelpers.TryMask, aMask);

            return reduced;
        }
    }

    public override IEnumerable<DigitFence> GetFencedDigits(GameState state, ISimplificationTracker tracker) => [];
}