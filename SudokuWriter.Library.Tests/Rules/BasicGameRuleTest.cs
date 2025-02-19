using System.Linq;
using NUnit.Framework;
using Shouldly;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Library.Tests.Rules;

[TestFixture]
[TestOf(typeof(BasicGameRule))]
public class BasicGameRuleTest
{
    [Test]
    public void EvaluateDefault()
    {
        var state = GameState.Default;
        BasicGameRule.Instance.Evaluate(state).ShouldBe(GameResult.Unknown);
    }

    [Test]
    public void EvaluateSingleSet()
    {
        var s = new GameStructure(4, 4, 4, 2, 2);
        var state = new GameState(Cells.CreateFilled(s).SetCell(0, 0, new CellValue(0)), s);
        BasicGameRule.Instance.Evaluate(state).ShouldBe(GameResult.Unknown);
    }

    [Test]
    public void EvaluatePairedRow()
    {
        var s = new GameStructure(4, 4, 4, 2, 2);
        var state = new GameState(
            Cells.CreateFilled(s)
                .SetCell(0, 0, new CellValue(0))
                .SetCell(2, 0, new CellValue(0)),
            s
        );
        BasicGameRule.Instance.Evaluate(state).ShouldBe(GameResult.Unsolvable);
    }

    [Test]
    public void EvaluatePairedCol()
    {
        var s = new GameStructure(4, 4, 4, 2, 2);
        var state = new GameState(
            Cells.CreateFilled(s)
                .SetCell(0, 0, new CellValue(0))
                .SetCell(0, 2, 0),
            s
        );
        BasicGameRule.Instance.Evaluate(state).ShouldBe(GameResult.Unsolvable);
    }

    [Test]
    public void EvaluatePairedBox()
    {
        var s = new GameStructure(4, 4, 4, 2, 2);
        var state = new GameState(
            Cells.CreateFilled(s)
                .SetCell(0, 0, 0)
                .SetCell(1, 1, 0),
            s
        );
        BasicGameRule.Instance.Evaluate(state).ShouldBe(GameResult.Unsolvable);
    }

    [Test]
    public void EvaluateSolved()
    {
        var s = new GameStructure(4, 4, 4, 2, 2);
        var state = new GameState(
            Cells.CreateFilled(s)
                .SetCell(0, 0, 0)
                .SetCell(0, 1, 1)
                .SetCell(0, 2, 2)
                .SetCell(0, 3, 3)
                .SetCell(1, 0, 2)
                .SetCell(1, 1, 3)
                .SetCell(1, 2, 0)
                .SetCell(1, 3, 1)
                .SetCell(2, 0, 1)
                .SetCell(2, 1, 2)
                .SetCell(2, 2, 3)
                .SetCell(2, 3, 0)
                .SetCell(3, 0, 3)
                .SetCell(3, 1, 0)
                .SetCell(3, 2, 1)
                .SetCell(3, 3, 2),
            s
        );
        BasicGameRule.Instance.Evaluate(state).ShouldBe(GameResult.Solved);
    }

    [Test]
    public void SimplifyNoneFails()
    {
        var s = new GameStructure(4, 4, 4, 2, 2);
        var state = new GameState(
            Cells.CreateFilled(s),
            s
        );
        BasicGameRule.Instance.TryReduce(state, TestSimplificationTracker.Instance.GetEmptyChain()).ShouldBeNull();
    }

    [Test]
    public void SimplifyFromOneSet()
    {
        var s = new GameStructure(4, 4, 4, 2, 2);
        var state = new GameState(
            Cells.CreateFilled(s)
                .SetCell(0, 0, 0),
            s
        );
        GameState? reducedState = BasicGameRule.Instance.TryReduce(state, TestSimplificationTracker.Instance.GetEmptyChain());
        reducedState.ShouldNotBeNull();
        reducedState.Value.Cells[0, 0].ShouldBe(1);

        reducedState.Value.Cells[0, 1].ShouldBe(14);
        reducedState.Value.Cells[0, 2].ShouldBe(14);
        reducedState.Value.Cells[0, 3].ShouldBe(14);

        reducedState.Value.Cells[1, 0].ShouldBe(14);
        reducedState.Value.Cells[2, 0].ShouldBe(14);
        reducedState.Value.Cells[3, 0].ShouldBe(14);

        reducedState.Value.Cells[1, 1].ShouldBe(14);

        reducedState.Value.Cells[2, 2].ShouldBe(15);
    }

    [Test]
    public void DigitFences()
    {
        var s = new GameStructure(4, 4, 4, 2, 2);
        CellsBuilder builder = CellsBuilder.CreateFilled(s);
        builder[0, 0] = new CellValue(0) | new CellValue(1) | new CellValue(2);
        var state = new GameState(builder.MoveToImmutable(), s);
        var fences = new BasicGameRule().GetFencedDigits(state, TestSimplificationTracker.Instance).ToList();
        fences.Count.ShouldBe(3);
        fences.ShouldAllBe(e => e.Digit == new CellValue(3));
        var row0 = state.Cells.GetRow(0).Box();
        fences.ShouldContain(e => row0.IsStrictSuperSetOf(e.Cells));
        var col0 = state.Cells.GetRow(0).Box();
        fences.ShouldContain(e => col0.IsStrictSuperSetOf(e.Cells));
        var box0 = state.Cells.GetRow(0).Box();
        fences.ShouldContain(e => box0.IsStrictSuperSetOf(e.Cells));
    }
}