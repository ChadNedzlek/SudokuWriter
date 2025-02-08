using NUnit.Framework;
using Shouldly;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Library.Tests.Rules;

[TestFixture]
[TestOf(typeof(DivLineRule))]
public class DivLineRuleTest
{
    private static readonly GameStructure s_structure = new(3, 3, 9, 3, 3);
    [Test]
    public void NothingTestIsUnknown()
    {
        var filled = Cells.CreateFilled(s_structure);
        var state = new GameState(filled, s_structure);
        var modLineRule = new DivLineRule([new([new([(0, 0), (0, 1), (0, 2), (1, 2), (1, 1), (1, 0), (2, 0), (2, 1), (2, 2)])])]);
        modLineRule.Evaluate(state).ShouldBe(GameResult.Unknown);        
    }
    
    [Test]
    public void InLineIsSolved()
    {
        var singular = Cells.FromDigits(new ushort[,] { { 0, 3, 6 }, { 7, 4, 1 }, { 2, 5, 8 } });
        var state = new GameState(singular, s_structure);
        var modLineRule = new DivLineRule([new([new([(0, 0), (0, 1), (0, 2), (1, 2), (1, 1), (1, 0), (2, 0), (2, 1), (2, 2)])])]);
        modLineRule.Evaluate(state).ShouldBe(GameResult.Solved);          
    }
    
    [Test]
    public void OutOfLineIsUnsolvable()
    {
        var singular = Cells.FromDigits(new ushort[,] { { 0, 3, 5 }, { 7, 4, 1 }, { 2, 6, 8 } });
        var state = new GameState(singular, s_structure);
        var modLineRule = new DivLineRule([new([new([(0, 0), (0, 1), (0, 2), (1, 2), (1, 1), (1, 0), (2, 0), (2, 1), (2, 2)])])]);
        modLineRule.Evaluate(state).ShouldBe(GameResult.Unsolvable);          
    }
    
    [Test]
    public void ReduceSingleEntry()
    {
        var filled = Cells.CreateFilled(s_structure).SetCell(0, 0, 0);
        var state = new GameState(filled, s_structure);
        var modLineRule = new DivLineRule([new([new([(0, 0), (0, 1), (0, 2), (1, 2), (1, 1), (1, 0), (2, 0), (2, 1), (2, 2)])])]);
        var reduced = modLineRule.TryReduce(state).ShouldNotBeNull();
        reduced.Cells[0,0].ToString().ShouldBe("1");
        reduced.Cells[0,1].ToString().ShouldBe("456789");
        reduced.Cells[0,2].ToString().ShouldBe("456789");
        
        reduced.Cells[1,0].ToString().ShouldBe("456789");
        reduced.Cells[1,1].ToString().ShouldBe("456789");
        reduced.Cells[1,2].ToString().ShouldBe("123");
        
        reduced.Cells[2,0].ToString().ShouldBe("123");
        reduced.Cells[2,1].ToString().ShouldBe("456789");
        reduced.Cells[2,2].ToString().ShouldBe("456789");
    }
}