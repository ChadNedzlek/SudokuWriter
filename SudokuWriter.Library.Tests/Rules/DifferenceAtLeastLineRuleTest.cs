using NUnit.Framework;
using Shouldly;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Library.Tests.Rules;

[TestFixture]
[TestOf(typeof(DifferenceAtLeastLineRuleBase<>))]
[TestOf(typeof(DifferenceAtLeastLineRule))]
public class DifferenceAtLeastLineRuleTest
{
    [Test]
    public void SuccessInUnboundState()
    {
        var structure = new GameStructure(2, 2, 4, 2, 2);
        var state = GameState.CreateFilled(structure);
        var rule = new DifferenceAtLeastLineRule([(0, 0), (0, 1), (1, 1), (1, 0)], 2);
        rule.Evaluate(state).ShouldBe(GameResult.Solved);
    }
    
    [Test]
    public void ViolationWithNearCells()
    {
        var structure = new GameStructure(2, 2, 4, 2, 2);
        var state = GameState.CreateFilled(structure);
        var cells = state.Cells
            .SetCell(0, 0, 0)
            .SetCell(0, 1, 1);
        state = state.WithCells(cells);
        var rule = new DifferenceAtLeastLineRule([(0, 0), (0, 1), (1, 1), (1, 0)], 2);
        rule.Evaluate(state).ShouldBe(GameResult.Unsolvable);
    }
    
    [Test]
    public void SuccessWithComplete()
    {
        var structure = new GameStructure(2, 2, 4, 2, 2);
        var state = GameState.CreateFilled(structure);
        var cells = state.Cells
            .SetCell(0, 0, 2)
            .SetCell(0, 1, 0)
            .SetCell(1, 1, 3)
            .SetCell(1, 0, 1);
        state = state.WithCells(cells);
        var rule = new DifferenceAtLeastLineRule([(0, 0), (0, 1), (1, 1), (1, 0)], 2);
        rule.Evaluate(state).ShouldBe(GameResult.Solved);
    }
    
    [Test]
    public void SimplifyPartial()
    {
        var structure = new GameStructure(2, 2, 4, 2, 2);
        var state = GameState.CreateFilled(structure);
        var cells = state.Cells
            .SetCell(0, 0, 2);
        state = state.WithCells(cells);
        var rule = new DifferenceAtLeastLineRule([(0, 0), (0, 1), (1, 1), (1, 0)], 2);
        GameState reduced = rule.TryReduce(state, TestSimplificationTracker.Instance.GetEmptyChain()).ShouldNotBeNull();
        reduced.Cells[0,1].ShouldBe(new CellValue(0).AsMask());
        reduced.Cells[1,1].ShouldBe(new CellValue(2).AsMask() | new CellValue(3).AsMask());
        reduced.Cells[1,0].ShouldBe(new CellValue(0).AsMask() | new CellValue(1).AsMask());
    }
}