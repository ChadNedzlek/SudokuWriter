using FluentAssertions;
using NUnit.Framework;

namespace SudokuWriter.Library.Tests;

[TestFixture]
[TestOf(typeof(BasicGameRule))]
public class BasicGameRuleTest
{
    [Test]
    public void EvaluateDefault()
    {
        var state = new GameState(Cells.CreateFilled());
        BasicGameRule.Instance.Evaluate(state).Should().Be(GameResult.Unknown);
    }

    [Test]
    public void EvaluateSingleSet()
    {
        var state = new GameState(Cells.CreateFilled(4, 4, 4).SetCell(0, 0, 0), 2, 2, 4);
        BasicGameRule.Instance.Evaluate(state).Should().Be(GameResult.Unknown);
    }

    [Test]
    public void EvaluatePairedRow()
    {
        var state = new GameState(
            Cells.CreateFilled(4, 4, 4)
                .SetCell(0, 0, 0)
                .SetCell(2, 0, 0),
            2,
            2,
            4
        );
        BasicGameRule.Instance.Evaluate(state).Should().Be(GameResult.Unsolvable);
    }

    [Test]
    public void EvaluatePairedCol()
    {
        var state = new GameState(
            Cells.CreateFilled(4, 4, 4)
                .SetCell(0, 0, 0)
                .SetCell(0, 2, 0),
            2,
            2,
            4
        );
        BasicGameRule.Instance.Evaluate(state).Should().Be(GameResult.Unsolvable);
    }

    [Test]
    public void EvaluatePairedBox()
    {
        var state = new GameState(
            Cells.CreateFilled(4, 4, 4)
                .SetCell(0, 0, 0)
                .SetCell(1, 1, 0),
            2,
            2,
            4
        );
        BasicGameRule.Instance.Evaluate(state).Should().Be(GameResult.Unsolvable);
    }

    [Test]
    public void EvaluateSolved()
    {
        var state = new GameState(
            Cells.CreateFilled(4, 4, 4)
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
            2,
            2,
            4
        );
        BasicGameRule.Instance.Evaluate(state).Should().Be(GameResult.Solved);
    }

    [Test]
    public void SimplifyNoneFails()
    {
        var state = new GameState(
            Cells.CreateFilled(4, 4, 4),
            2,
            2,
            4
        );
        BasicGameRule.Instance.TryReduce(state).Should().BeNull();
    }

    [Test]
    public void SimplifyFromOneSet()
    {
        var state = new GameState(
            Cells.CreateFilled(4, 4, 4)
                .SetCell(0, 0, 0),
            2,
            2,
            4
        );
        GameState? reducedState = BasicGameRule.Instance.TryReduce(state);
        reducedState.Value.Should().NotBeNull();
        reducedState.Value.Cells.GetMask(0, 0).Should().Be(1);

        reducedState.Value.Cells.GetMask(0, 1).Should().Be(14);
        reducedState.Value.Cells.GetMask(0, 2).Should().Be(14);
        reducedState.Value.Cells.GetMask(0, 3).Should().Be(14);

        reducedState.Value.Cells.GetMask(1, 0).Should().Be(14);
        reducedState.Value.Cells.GetMask(2, 0).Should().Be(14);
        reducedState.Value.Cells.GetMask(3, 0).Should().Be(14);

        reducedState.Value.Cells.GetMask(1, 1).Should().Be(14);

        reducedState.Value.Cells.GetMask(2, 2).Should().Be(15);
    }
}