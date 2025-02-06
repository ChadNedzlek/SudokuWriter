using NUnit.Framework;
using Shouldly;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Library.Tests.Rules;

[TestFixture]
[TestOf(typeof(KnightsMoveRule))]
public class KnightsMoveRuleTest
{
    [Test]
    public void NoConflictOnEmptyGrid()
    {
        var structure = new GameStructure(3, 3, 3, 1, 1);
        var state = new GameState(Cells.CreateFilled(structure), structure);
        new KnightsMoveRule().Evaluate(state).ShouldBe(GameResult.Solved);
    }

    [Test]
    public void ConflictWithKnightMove()
    {
        var structure = new GameStructure(3, 3, 3, 1, 1);
        var state = new GameState(Cells.CreateFilled(structure).SetCell(0, 0, 0).SetCell(2, 1, 0), structure);
        new KnightsMoveRule().Evaluate(state).ShouldBe(GameResult.Unsolvable);
    }

    [Test]
    public void SimplyFromSetCell()
    {
        var structure = new GameStructure(3, 3, 3, 1, 1);
        var state = new GameState(Cells.CreateFilled(structure).SetCell(0, 0, 0), structure);
        GameState reduced = new KnightsMoveRule().TryReduce(state).ShouldNotBeNull();
        reduced.Cells[2, 1].ShouldBe(CellValueMask.All(structure.Digits) & ~new CellValue(0).AsMask());
    }
}