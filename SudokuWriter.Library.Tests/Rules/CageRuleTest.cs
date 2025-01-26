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
        cells[0, 1] = Cells.GetDigitMask(0);
        var reducedState = new CageRule(5, [(0, 0), (0, 1)]).TryReduce(new GameState(cells.MoveToImmutable(), s)).ShouldNotBeNull();
        Cells.GetDigitDisplay(reducedState.Cells[0,0]).ShouldBe("4");
    }

    [Test]
    public void Basic2CellHighValueCheck()
    {
        GameStructure s = new(1, 2, 9, 1, 2);
        var cells = CellsBuilder.CreateFilled(s);
        cells[0, 1] = Cells.GetDigitMask(3);
        var reducedState = new CageRule(10, [(0, 0), (0, 1)]).TryReduce(new GameState(cells.MoveToImmutable(), s)).ShouldNotBeNull();
        Cells.GetDigitDisplay(reducedState.Cells[0,0]).ShouldBe("6");
    }

    [Test]
    public void MultiplePotentialValueCheck()
    {
        GameStructure s = new(1, 2, 9, 1, 2);
        var cells = CellsBuilder.CreateFilled(s);
        cells[0, 1] = (ushort)(Cells.GetDigitMask(3) | Cells.GetDigitMask(4) | Cells.GetDigitMask(6));
        var reducedState = new CageRule(10, [(0, 0), (0, 1)]).TryReduce(new GameState(cells.MoveToImmutable(), s)).ShouldNotBeNull();
        // Technically the 4 is "wrong", but that requires an "O(Cells^2 * Digits)" algorithm instead of an O(Digits) one
        Cells.GetDigitDisplay(reducedState.Cells[0,0]).ShouldBe("3456");
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
        cells[0, 1] = Cells.GetDigitMask(1);
        cells[0, 0] = Cells.GetDigitMask(4);
        new CageRule(7, [(0, 0), (0, 1)]).Evaluate(new GameState(cells.MoveToImmutable(), s)).ShouldBe(GameResult.Solved);
    }

    [Test]
    public void CageCannotBeHighEnoughUnsolvable()
    {
        GameStructure s = new(1, 2, 9, 1, 2);
        var cells = CellsBuilder.CreateFilled(s);
        cells[0, 1] = Cells.GetDigitMask(1);
        new CageRule(12, [(0, 0), (0, 1)]).Evaluate(new GameState(cells.MoveToImmutable(), s)).ShouldBe(GameResult.Unsolvable);
    }
}