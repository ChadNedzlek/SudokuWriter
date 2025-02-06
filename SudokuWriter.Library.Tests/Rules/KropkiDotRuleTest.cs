using NUnit.Framework;
using Shouldly;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Library.Tests.Rules;

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
    public void SimplySequentialFromSetCell()
    {
        var structure = new GameStructure(2, 2, 4, 2, 2);
        var cells = Cells.CreateFilled(structure).ToBuilder();
        cells[0, 0] = new CellValue(0) | new CellValue(1);
        var state = new GameState(cells.MoveToImmutable(), structure);
        GameState reduced = new KropkiDotRule(doubles: [], sequential: [(0,0)]).TryReduce(state).ShouldNotBeNull();
        reduced.Cells[0, 0].ToString().ShouldBe("12");
        reduced.Cells[0, 1].ToString().ShouldBe("123");
        reduced.Cells[1, 0].ToString().ShouldBe("1234");
        reduced.Cells[1, 1].ToString().ShouldBe("1234");
    }

    [Test]
    public void SimplyDoubleFromSetCell()
    {
        var structure = new GameStructure(2, 2, 4, 2, 2);
        var cells = Cells.CreateFilled(structure).ToBuilder();
        cells[0, 0] = new CellValue(0) | new CellValue(1);
        var state = new GameState(cells.MoveToImmutable(), structure);
        GameState reduced = new KropkiDotRule(doubles: [(0,0)], sequential: []).TryReduce(state).ShouldNotBeNull();
        reduced.Cells[0, 0].ToString().ShouldBe("12");
        reduced.Cells[0, 1].ToString().ShouldBe("124");
        reduced.Cells[1, 0].ToString().ShouldBe("1234");
        reduced.Cells[1, 1].ToString().ShouldBe("1234");
    }
}