using NUnit.Framework;
using Shouldly;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Library.Tests.Rules;

public class ParityCellRuleTests
{
    [Test]
    public void DetectFailureInEvenViolation()
    {
        var structure = new GameStructure(2, 2, 2, 1, 2);
        var parityCellRule = new ParityCellRule([(0, 0)], []);
        var solved = new GameState(
            Cells.CreateFilled(structure)
                .SetCell(0, 0, 0)
                .SetCell(0, 1, 1)
                .SetCell(1, 0, 1)
                .SetCell(1, 1, 0),
            structure
        );

        parityCellRule.Evaluate(solved).ShouldBe(GameResult.Unsolvable);
    }

    [Test]
    public void DetectFailureInOddViolation()
    {
        var structure = new GameStructure(2, 2, 2, 1, 2);
        var parityCellRule = new ParityCellRule([(0, 0)], []);
        var solved = new GameState(
            Cells.CreateFilled(structure)
                .SetCell(0, 0, 0)
                .SetCell(0, 1, 1)
                .SetCell(1, 0, 1)
                .SetCell(1, 1, 0),
            structure
        );
        parityCellRule.Evaluate(solved).ShouldBe(GameResult.Unsolvable);
    }

    [Test]
    public void SuccessInEvenMatch()
    {
        var structure = new GameStructure(2, 2, 2, 1, 2);
        var engine = new GameEngine(new GameState(Cells.CreateFilled(structure), structure), new ParityCellRule([(1, 0)], []));
        GameState solved = engine.InitialState.WithCells(
            engine.InitialState.Cells
                .SetCell(0, 0, 0)
                .SetCell(0, 1, 1)
                .SetCell(1, 0, 1)
                .SetCell(1, 1, 0)
        );
        engine.Evaluate(solved, out _, out _).ShouldBe(GameResult.Solved);
    }

    [Test]
    public void SuccessInOddMatch()
    {
        var structure = new GameStructure(2, 2, 2, 1, 2);
        var engine = new GameEngine(new GameState(Cells.CreateFilled(structure), structure), new ParityCellRule([], [(0, 0)]));
        GameState solved = engine.InitialState.WithCells(
            engine.InitialState.Cells
                .SetCell(0, 0, 0)
                .SetCell(0, 1, 1)
                .SetCell(1, 0, 1)
                .SetCell(1, 1, 0)
        );
        engine.Evaluate(solved, out _, out _).ShouldBe(GameResult.Solved);
    }

    [Test]
    public void SuccessInUnboundState()
    {
        var structure = new GameStructure(2, 2, 2, 1, 2);
        var engine = new GameEngine(new GameState(Cells.CreateFilled(structure), structure), new ParityCellRule([(1, 0)], [(0, 0)]));
        GameState solved = engine.InitialState;
        engine.Evaluate(solved, out _, out _).ShouldBe(GameResult.Solved);
    }

    [Test]
    public void SimplifyInUnboundState()
    {
        var structure = new GameStructure(2, 2, 2, 1, 2);
        var engine = new GameEngine(new GameState(Cells.CreateFilled(structure), structure), new ParityCellRule([(1, 0)], [(0, 0)]));
        GameState reduced = engine.Rules[0].TryReduce(engine.InitialState, TestSimplificationTracker.Instance.GetEmptyChain()).ShouldNotBeNull();
        reduced.Cells.GetSingle(0, 0).ShouldBe(0);
        reduced.Cells.GetSingle(1, 0).ShouldBe(1);
    }
}