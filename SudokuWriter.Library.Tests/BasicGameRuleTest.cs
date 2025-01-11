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
        GameState state = new GameState(cells: Cells.CreateFilled());
        BasicGameRule.Instance.Evaluate(state).Should().Be(GameResult.Unknown);
    }
    
    [Test]
    public void EvaluateSingleSet()
    {
        GameState state = new GameState(cells: Cells.CreateFilled(rows: 4, columns: 4, digits: 4).SetCell(0,0,0), boxRows: 2, boxColumns: 2, digits: 4);
        BasicGameRule.Instance.Evaluate(state).Should().Be(GameResult.Unknown);
    }
    
    [Test]
    public void EvaluatePairedRow()
    {
        GameState state = new GameState(
            cells: Cells.CreateFilled(rows: 4, columns: 4, digits: 4)
                .SetCell(0, 0, 0)
                .SetCell(2, 0, 0),
            boxRows: 2,
            boxColumns: 2,
            digits: 4
        );
        BasicGameRule.Instance.Evaluate(state).Should().Be(GameResult.Unsolvable);
    }
    
    [Test]
    public void EvaluatePairedCol()
    {
        GameState state = new GameState(
            cells: Cells.CreateFilled(rows: 4, columns: 4, digits: 4)
                .SetCell(0, 0, 0)
                .SetCell(0, 2, 0),
            boxRows: 2,
            boxColumns: 2,
            digits: 4
        );
        BasicGameRule.Instance.Evaluate(state).Should().Be(GameResult.Unsolvable);
    }
    
    [Test]
    public void EvaluatePairedBox()
    {
        GameState state = new GameState(
            cells: Cells.CreateFilled(rows: 4, columns: 4, digits: 4)
                .SetCell(0, 0, 0)
                .SetCell(1, 1, 0),
            boxRows: 2,
            boxColumns: 2,
            digits: 4
        );
        BasicGameRule.Instance.Evaluate(state).Should().Be(GameResult.Unsolvable);
    }
    
    [Test]
    public void EvaluateSolved()
    {
        GameState state = new GameState(
            cells: Cells.CreateFilled(rows: 4, columns: 4, digits: 4)
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
            boxRows: 2,
            boxColumns: 2,
            digits: 4
        );
        BasicGameRule.Instance.Evaluate(state).Should().Be(GameResult.Solved);
    }
    
    [Test]
    public void SimplifyNoneFails()
    {
        GameState state = new GameState(
            cells: Cells.CreateFilled(rows: 4, columns: 4, digits: 4),
            boxRows: 2,
            boxColumns: 2,
            digits: 4
        );
        BasicGameRule.Instance.TryReduce(ref state).Should().BeFalse();
    }
    
    [Test]
    public void SimplifyFromOneSet()
    {
        GameState state = new GameState(
            cells: Cells.CreateFilled(rows: 4, columns: 4, digits: 4)
                .SetCell(0, 0, 0),
            boxRows: 2,
            boxColumns: 2,
            digits: 4
        );
        BasicGameRule.Instance.TryReduce(ref state).Should().BeTrue();
        state.Cells.GetMask(0, 0).Should().Be(1);
        
        state.Cells.GetMask(0, 1).Should().Be(14);
        state.Cells.GetMask(0, 2).Should().Be(14);
        state.Cells.GetMask(0, 3).Should().Be(14);
        
        state.Cells.GetMask(1, 0).Should().Be(14);
        state.Cells.GetMask(2, 0).Should().Be(14);
        state.Cells.GetMask(3, 0).Should().Be(14);
        
        state.Cells.GetMask(1, 1).Should().Be(14);
        
        state.Cells.GetMask(2, 2).Should().Be(15);
    }
}