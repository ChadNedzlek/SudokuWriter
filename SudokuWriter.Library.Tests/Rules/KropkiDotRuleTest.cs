using NUnit.Framework;
using Shouldly;
using SudokuWriter.Library.Rules;

namespace SudokuWriter.Library.Tests.Rules;

[TestFixture]
[TestOf(typeof(KropkiDotRule))]
public class KropkiDotRuleTest
{
    [Test]
    public void NoConflictOnEmptyGrid()
    {
        var structure = new GameStructure(2, 2, 4, 2, 2);
        var state = new GameState(Cells.CreateFilled(structure), structure);
        new KropkiDotRule([(0,0)], [(2,0)]).Evaluate(state).ShouldBe(GameResult.Solved);
    }

    [Test]
    public void ConflictWithNoDouble()
    {
        var structure = new GameStructure(2, 2, 4, 2, 2);
        var state = new GameState(Cells.CreateFilled(structure).SetCell(0, 0, 13).SetCell(0, 1, 13), structure);
        new KropkiDotRule([(0,0)], [(2,0)]).Evaluate(state).ShouldBe(GameResult.Unsolvable);
    }

    [Test]
    public void ConflictWithNoSequential()
    {
        var structure = new GameStructure(2, 2, 4, 2, 2);
        var state = new GameState(Cells.CreateFilled(structure).SetCell(1, 0, 9).SetCell(1, 1, 9), structure);
        new KropkiDotRule([(0,0)], [(2,0)]).Evaluate(state).ShouldBe(GameResult.Unsolvable);
    }

    [Test]
    public void SimplyFromSetCell()
    {
        var structure = new GameStructure(2, 2, 4, 2, 2);
        var cells = Cells.CreateFilled(structure).ToBuilder();
        cells[1, 0] = 13;
        cells[1, 1] = 13;
        var state = new GameState(cells.MoveToImmutable(), structure);
        GameState reduced = new KropkiDotRule(doubles: [(0,0)], sequential: [(2,0)]).TryReduce(state).ShouldNotBeNull();
        Cells.GetDigitDisplay(reduced.Cells[0, 0]).ShouldBe("124");
        Cells.GetDigitDisplay(reduced.Cells[0, 1]).ShouldBe("124");
        Cells.GetDigitDisplay(reduced.Cells[1, 0]).ShouldBe("34");
        Cells.GetDigitDisplay(reduced.Cells[1, 1]).ShouldBe("34");
    }
}