using NUnit.Framework;
using Shouldly;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Library.Tests.Rules;

[TestFixture]
[TestOf(typeof(ThermoLineRule))]
public class ThermoLineRuleTest
{
    [Test]
    public void SolvedIsSolved()
    {
        GameStructure s = new(2, 2, 4, 2, 2);
        var cells = CellsBuilder.CreateFilled(s);
        cells[0, 0] = new CellValue(0).AsMask();
        cells[0, 1] = new CellValue(1).AsMask();
        cells[1, 1] = new CellValue(2).AsMask();
        cells[1, 0] = new CellValue(3).AsMask();
        var state = new GameState(cells.MoveToImmutable(), s);
        var thermoRule = new ThermoLineRule([new([new([(0, 0), (0, 1), (1, 1), (1, 0)])])]);
        thermoRule.Evaluate(state).ShouldBe(GameResult.Solved);
    }
    
    [Test]
    public void UnsolvableIsUnsolvable()
    {
        GameStructure s = new(2, 2, 4, 2, 2);
        var cells = CellsBuilder.CreateFilled(s);
        cells[0, 0] = new CellValue(2) | new CellValue(3);
        cells[0, 1] = new CellValue(0) | new CellValue(1);
        var state = new GameState(cells.MoveToImmutable(), s);
        var thermoRule = new ThermoLineRule([new([new([(0, 0), (0, 1)])])]);
        thermoRule.Evaluate(state).ShouldBe(GameResult.Unsolvable);
    }

    [Test]
    public void PartialReduce()
    {
        GameStructure s = new(2, 2, 4, 2, 2);
        var state = new GameState(Cells.CreateFilled(s), s);
        var thermoRule = new ThermoLineRule([new([new([(0, 0), (0, 1)])])]);
        var reducedState = thermoRule.TryReduce(state).ShouldNotBeNull();
        reducedState.Cells[0, 0].ShouldBe(new CellValue(0) | new CellValue(1) | new CellValue(2));
        reducedState.Cells[0, 1].ShouldBe(new CellValue(1) | new CellValue(2) | new CellValue(3));
    }
}