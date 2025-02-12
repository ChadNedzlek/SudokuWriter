using System.Collections.Generic;
using System.Collections.Immutable;

namespace VaettirNet.SudokuWriter.Library.Rules;

public abstract class TriLineRule<T> : LineRule<T>
    where T : ILineRule<T>
{
    protected TriLineRule(ImmutableArray<BranchingRuleLine> lines) : base(lines)
    {
    }

    public override GameResult Evaluate(GameState state)
    {
        Cells cells = state.Cells;
        GameResult result = GameResult.Solved;
        foreach (BranchingRuleLine line in Lines)
        {
            for (int iBranch = 0; iBranch < line.Branches.Length; iBranch++)
            {
                LineRuleSegment branch = line.Branches[iBranch];
                var a = cells.GetEmptyReferences();
                var b = cells.GetEmptyReferences();
                var c = cells.GetEmptyReferences();
                for (int iCell = 0; iCell < branch.Cells.Length; iCell++)
                {
                    scoped ref ReadOnlyMultiRef<CellValueMask> x = ref a;
                    switch(iCell % 3)
                    {
                        case 1: x = ref b; break;
                        case 2: x = ref c; break;
                    };
                    x.Include(in cells[branch.Cells[iCell]]);
                }

                result = (result, EvaluateGroup(a, b, c)) switch
                {
                    (GameResult.Unsolvable, _) => GameResult.Unsolvable,
                    (_, GameResult.Unsolvable) => GameResult.Unsolvable,
                    (GameResult.Unknown, _) => GameResult.Unknown,
                    (_, GameResult.Unknown) => GameResult.Unknown,
                    _ => result,
                };
            }
        }

        return result;
    }

    public override GameState? TryReduce(GameState state, ISimplificationChain chain)
    {
        CellsBuilder cells = state.Cells.ToBuilder();
        bool reduced = false;
        foreach (BranchingRuleLine line in Lines)
        {
            foreach (LineRuleSegment branch in line.Branches)
            {
                var a = cells.GetEmptyReferences();
                var b = cells.GetEmptyReferences();
                var c = cells.GetEmptyReferences();
                for (int iCell = 0; iCell < branch.Cells.Length; iCell++)
                {
                    scoped ref MultiRef<CellValueMask> x = ref a;
                    switch(iCell % 3)
                    {
                        case 1: x = ref b; break;
                        case 2: x = ref c; break;
                    };
                    x.Include(ref cells[branch.Cells[iCell]]);
                }

                reduced = ReduceGroups(ref a, ref b, ref c);
            }
        }

        return reduced ? state.WithCells(cells.MoveToImmutable()) : null;
    }

    protected abstract GameResult EvaluateGroup(in ReadOnlyMultiRef<CellValueMask> a, in ReadOnlyMultiRef<CellValueMask> b, in ReadOnlyMultiRef<CellValueMask> c);
    protected abstract bool ReduceGroups(ref MultiRef<CellValueMask> a, ref MultiRef<CellValueMask> b, ref MultiRef<CellValueMask> c);

    public override IEnumerable<MutexGroup> GetMutualExclusionGroups(GameState state, ISimplificationTracker tracker) => [];
}