using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using NUnit.Framework;
using Shouldly;

namespace SudokuWriter.Library.Tests;

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
        var state = new GameState(Cells.CreateFilled(s).SetCell(0, 0, 0), s);
        BasicGameRule.Instance.Evaluate(state).ShouldBe(GameResult.Unknown);
    }

    [Test]
    public void EvaluatePairedRow()
    {
        var s = new GameStructure(4, 4, 4, 2, 2);
        var state = new GameState(
            Cells.CreateFilled(s)
                .SetCell(0, 0, 0)
                .SetCell(2, 0, 0),
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
                .SetCell(0, 0, 0)
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
        BasicGameRule.Instance.TryReduce(state).ShouldBeNull();
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
        GameState? reducedState = BasicGameRule.Instance.TryReduce(state);
        reducedState.ShouldNotBeNull();
        reducedState.Value.Cells.GetMask(0, 0).ShouldBe(1);

        reducedState.Value.Cells.GetMask(0, 1).ShouldBe(14);
        reducedState.Value.Cells.GetMask(0, 2).ShouldBe(14);
        reducedState.Value.Cells.GetMask(0, 3).ShouldBe(14);

        reducedState.Value.Cells.GetMask(1, 0).ShouldBe(14);
        reducedState.Value.Cells.GetMask(2, 0).ShouldBe(14);
        reducedState.Value.Cells.GetMask(3, 0).ShouldBe(14);

        reducedState.Value.Cells.GetMask(1, 1).ShouldBe(14);

        reducedState.Value.Cells.GetMask(2, 2).ShouldBe(15);
    }
}

public static class AssertHelpers
{
    public static void ShouldBe(
        this ushort actual,
        int expected,
        string customMessage = null)
    {
        ((int)actual).ShouldBe(expected, customMessage);
    }
}