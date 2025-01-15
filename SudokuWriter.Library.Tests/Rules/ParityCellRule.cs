using NUnit.Framework;
using Shouldly;

namespace SudokuWriter.Library.Tests.Rules;

public class ParityCellRule
{
    [Test]
    public void DetectFailureInEvenViolation()
    {
        var structure = new GameStructure(2, 2, 2, 1, 2);
        var engine = new GameEngine(new GameState(Cells.CreateFilled(structure), structure), new Library.Rules.ParityCellRule(evenCells: [(0, 0)], oddCells: []));
        var solved = engine.InitialState.WithCells(engine.InitialState.Cells
            .SetCell(0, 0, 0)
            .SetCell(0, 1, 1)
            .SetCell(1, 0, 1)
            .SetCell(1, 1, 0)
        );
        engine.Evaluate(solved, out _, out _).ShouldBe(GameResult.Unsolvable);
    }
    
    [Test]
    public void DetectFailureInOddViolation()
    {
        var structure = new GameStructure(2, 2, 2, 1, 2);
        var engine = new GameEngine(new GameState(Cells.CreateFilled(structure), structure), new Library.Rules.ParityCellRule(evenCells: [], oddCells: [(1,0)]));
        var solved = engine.InitialState.WithCells(engine.InitialState.Cells
            .SetCell(0, 0, 0)
            .SetCell(0, 1, 1)
            .SetCell(1, 0, 1)
            .SetCell(1, 1, 0)
        );
        engine.Evaluate(solved, out _, out _).ShouldBe(GameResult.Unsolvable);
    }
    
    [Test]
    public void SuccessInEvenMatch()
    {
        var structure = new GameStructure(2, 2, 2, 1, 2);
        var engine = new GameEngine(new GameState(Cells.CreateFilled(structure), structure), new Library.Rules.ParityCellRule(evenCells: [(1, 0)], oddCells: []));
        var solved = engine.InitialState.WithCells(engine.InitialState.Cells
            .SetCell(0, 0, 0)
            .SetCell(0, 1, 1)
            .SetCell(1, 0, 1)
            .SetCell(1, 1, 0)
        );
        engine.Evaluate(solved, out _, out _).ShouldBe(GameResult.Unsolvable);
    }
    
    [Test]
    public void SuccessInOddMatch()
    {
        var structure = new GameStructure(2, 2, 2, 1, 2);
        var engine = new GameEngine(new GameState(Cells.CreateFilled(structure), structure), new Library.Rules.ParityCellRule(evenCells: [], oddCells: [(0,0)]));
        var solved = engine.InitialState.WithCells(engine.InitialState.Cells
            .SetCell(0, 0, 0)
            .SetCell(0, 1, 1)
            .SetCell(1, 0, 1)
            .SetCell(1, 1, 0)
        );
        engine.Evaluate(solved, out _, out _).ShouldBe(GameResult.Unsolvable);
    }
    
    [Test]
    public void SuccessInUnboundState()
    {
        var structure = new GameStructure(2, 2, 2, 1, 2);
        var engine = new GameEngine(new GameState(Cells.CreateFilled(structure), structure), new Library.Rules.ParityCellRule(evenCells: [(1,0)], oddCells: [(0,0)]));
        var solved = engine.InitialState;
        engine.Evaluate(solved, out _, out _).ShouldBe(GameResult.Solved);
    }
    
    [Test]
    public void SimplifyInUnboundState()
    {
        var structure = new GameStructure(2, 2, 2, 1, 2);
        var engine = new GameEngine(new GameState(Cells.CreateFilled(structure), structure), new Library.Rules.ParityCellRule(evenCells: [(1,0)], oddCells: [(0,0)]));
        var reduced = engine.Rules[0].TryReduce(engine.InitialState).ShouldNotBeNull();
        reduced.Cells.GetSingle(0, 0).ShouldBe(0);
        reduced.Cells.GetSingle(1, 0).ShouldBe(1);
    }
}