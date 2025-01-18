using NUnit.Framework;
using Shouldly;
using SudokuWriter.Library.Rules;

namespace SudokuWriter.Library.Tests.Rules;

[TestFixture]
[TestOf(typeof(RenbanLine))]
public class RenbanLineTest
{
    [Test]
    public void UnboundStateIsUnknown()
    {
        GameStructure structure = new(3, 3, 9, 3, 3);
        GameState state = GameState.CreateFilled(structure);
        new RenbanLine((0,0),(1,1),(2,2)).Evaluate(state).ShouldBe(GameResult.Unknown);
    }
    
    [Test]
    public void SolvedStateIsSolved()
    {
        GameStructure structure = new(3, 3, 9, 3, 3);
        var cells = Cells.FromDigits(new ushort[,] { { 4, 1, 2 }, { 3, 5, 7 }, { 8, 9, 6 } });
        GameState state = new GameState(cells, structure);
        new RenbanLine((0,0),(1,1),(2,2)).Evaluate(state).ShouldBe(GameResult.Solved);
    }
    
    [Test]
    public void PartialSolveLowIsSimplified()
    {
        GameStructure structure = new(3, 3, 9, 3, 3);
        GameState state = GameState.CreateFilled(structure);
        state = state.WithCells(
            state.Cells
                .SetCell(1, 1, 3)
        );
        var reduced = new RenbanLine((0,0),(1,1),(2,2)).TryReduce(state).ShouldNotBeNull();
        int nearCells = Cells.GetDigitMask(1) | Cells.GetDigitMask(2) | Cells.GetDigitMask(4) | Cells.GetDigitMask(5);
        reduced.Cells[0,0].ShouldBe(nearCells);
        reduced.Cells[2,2].ShouldBe(nearCells);
    }
    
    [Test]
    public void PartialSolveHighIsSimplified()
    {
        GameStructure structure = new(3, 3, 9, 3, 3);
        GameState state = GameState.CreateFilled(structure);
        state = state.WithCells(
            state.Cells
                .SetCell(1, 1, 5)
        );
        var reduced = new RenbanLine((0,0),(1,1),(2,2)).TryReduce(state).ShouldNotBeNull();
        int nearCells = Cells.GetDigitMask(3) | Cells.GetDigitMask(4) | Cells.GetDigitMask(6) | Cells.GetDigitMask(7);
        reduced.Cells[0,0].ShouldBe(nearCells);
        reduced.Cells[2,2].ShouldBe(nearCells);
    }
    
    [Test]
    public void MissingPossibilityRemovesSegments()
    {
        GameStructure structure = new(3, 3, 9, 3, 3);
        ushort no2 = (ushort)(Cells.GetAllDigitsMask(structure.Digits) & ~Cells.GetDigitMask(2));
        ushort above2 = (ushort)(no2 & (~0 << 3));
        var cells = Cells.FromMasks(new[,] { { no2, no2, no2 }, { no2, no2, no2 }, { no2, no2, no2 } });
        GameState state = new GameState(cells, structure);
        var reduced = new RenbanLine((0,0),(1,1),(2,2)).TryReduce(state).ShouldNotBeNull();
        reduced.Cells[0,0].ShouldBe(above2);
        reduced.Cells[1,1].ShouldBe(above2);
        reduced.Cells[2,2].ShouldBe(above2);
    }
}