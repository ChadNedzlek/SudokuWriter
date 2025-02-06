using NUnit.Framework;
using Shouldly;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Library.Tests.Rules;

[TestFixture]
[TestOf(typeof(CageRule))]
public class CageRuleTest
{
    [Test]
    public void Basic2CellLowValueCheck()
    {
        GameStructure s = new(1, 2, 9, 1, 2);
        var cells = CellsBuilder.CreateFilled(s);
        cells[0, 1] = new CellValue(0).AsMask();
        var reducedState = new CageRule(5, [(0, 0), (0, 1)]).TryReduce(new GameState(cells.MoveToImmutable(), s)).ShouldNotBeNull();
        reducedState.Cells[0,0].ToString().ShouldBe("4");
    }

    [Test]
    public void Basic2CellHighValueCheck()
    {
        GameStructure s = new(1, 2, 9, 1, 2);
        var cells = CellsBuilder.CreateFilled(s);
        cells[0, 1] = new CellValue(3).AsMask();
        var reducedState = new CageRule(10, [(0, 0), (0, 1)]).TryReduce(new GameState(cells.MoveToImmutable(), s)).ShouldNotBeNull();
        reducedState.Cells[0,0].ToString().ShouldBe("6");
    }

    [Test]
    public void MultiplePotentialValueCheck()
    {
        GameStructure s = new(1, 2, 9, 1, 2);
        var cells = CellsBuilder.CreateFilled(s);
        cells[0, 1] = new CellValue(3) | new CellValue(4) | new CellValue(6);
        var reducedState = new CageRule(10, [(0, 0), (0, 1)]).TryReduce(new GameState(cells.MoveToImmutable(), s)).ShouldNotBeNull();
        // Technically the 4 is "wrong", but that requires an "O(Cells^2 * Digits)" algorithm instead of an O(Digits) one
        reducedState.Cells[0,0].ToString().ShouldBe("3456");
    }

    [Test]
    public void FilledStructSolvable()
    {
        GameStructure s = new(1, 2, 9, 1, 2);
        var cells = CellsBuilder.CreateFilled(s);
        new CageRule(10, [(0, 0), (0, 1)]).Evaluate(new GameState(cells.MoveToImmutable(), s)).ShouldBe(GameResult.Unknown);
    }

    [Test]
    public void CompletedCageSolved()
    {
        GameStructure s = new(1, 2, 9, 1, 2);
        var cells = CellsBuilder.CreateFilled(s);
        cells[0, 1] = new CellValue(1).AsMask();
        cells[0, 0] = new CellValue(4).AsMask();
        new CageRule(7, [(0, 0), (0, 1)]).Evaluate(new GameState(cells.MoveToImmutable(), s)).ShouldBe(GameResult.Solved);
    }

    [Test]
    public void CageCannotBeHighEnoughUnsolvable()
    {
        GameStructure s = new(1, 2, 9, 1, 2);
        var cells = CellsBuilder.CreateFilled(s);
        cells[0, 1] = new CellValue(1).AsMask();
        new CageRule(12, [(0, 0), (0, 1)]).Evaluate(new GameState(cells.MoveToImmutable(), s)).ShouldBe(GameResult.Unsolvable);
    }
}