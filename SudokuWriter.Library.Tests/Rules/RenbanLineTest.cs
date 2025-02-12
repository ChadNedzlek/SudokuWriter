using NUnit.Framework;
using Shouldly;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Library.Tests.Rules;

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
        var reduced = new RenbanLine((0,0),(1,1),(2,2)).TryReduce(state, TestSimplificationTracker.Instance.GetEmptyChain()).ShouldNotBeNull();
        CellValueMask nearCells = new CellValue(1) | new CellValue(2) | new CellValue(4) | new CellValue(5);
        reduced.Cells[0,0].ShouldBe(nearCells);
        reduced.Cells[1,1].ShouldBe(new CellValue(3).AsMask());
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
        var reduced = new RenbanLine((0,0),(1,1),(2,2)).TryReduce(state, TestSimplificationTracker.Instance.GetEmptyChain()).ShouldNotBeNull();
        CellValueMask nearCells = new CellValue(3) | new CellValue(4) | new CellValue(6) | new CellValue(7);
        reduced.Cells[0,0].ShouldBe(nearCells);
        reduced.Cells[1,1].ShouldBe(new CellValue(5).AsMask());
        reduced.Cells[2,2].ShouldBe(nearCells);
    }
    
    [Test]
    public void PartialSolveMidIsSimplified()
    {
        GameStructure structure = new(3, 3, 9, 3, 3);
        GameState state = GameState.CreateFilled(structure);
        state = state.WithCells(
            state.Cells
                .SetCell(1, 0, 1)
                .SetCell(1, 2, 3)
        );
        var reduced = new RenbanLine((0, 0), (1, 0), (1, 1), (1, 2), (2, 2)).TryReduce(state, TestSimplificationTracker.Instance.GetEmptyChain()).ShouldNotBeNull();
        CellValueMask nearCells = new CellValue(0) | new CellValue(2) | new CellValue(4) | new CellValue(5);
        reduced.Cells[0,0].ShouldBe(nearCells);
        reduced.Cells[1,1].ShouldBe(nearCells);
        reduced.Cells[2,2].ShouldBe(nearCells);
    }
    
    [Test]
    public void MissingPossibilityRemovesSegments()
    {
        GameStructure structure = new(3, 3, 9, 3, 3);
        CellValueMask all = CellValueMask.All(structure.Digits);
        CellValueMask no2 = all & ~new CellValue(2).AsMask();
        CellValueMask above2 = no2 & (all << 3);
        var cells = Cells.FromMasks(new[,] { { no2, no2, no2 }, { no2, no2, no2 }, { no2, no2, no2 } });
        GameState state = new GameState(cells, structure);
        var reduced = new RenbanLine((0,0),(1,1),(2,2)).TryReduce(state, TestSimplificationTracker.Instance.GetEmptyChain()).ShouldNotBeNull();
        reduced.Cells[0,0].ShouldBe(above2);
        reduced.Cells[1,1].ShouldBe(above2);
        reduced.Cells[2,2].ShouldBe(above2);
    }

    [Test]
    public void FullEngineTest()
    {
        var state = GameState.Default;
        state = state.WithCells(state.Cells.SetCell(4, 3, 1).SetCell(4, 5, 3));
        var engine = GameEngine.Default.AddRule(new RenbanLine((3, 3), (4, 3), (4, 4), (4, 5), (5, 5)));
        var chain = TestSimplificationTracker.Instance.GetEmptyChain();
        var simplified = engine.SimplifyState(state, chain);
        simplified.Cells.Any(m => m == CellValueMask.None).ShouldBeFalse();
        simplified.Cells[3,3].ToString().ShouldBe("1356");
        simplified.Cells[4,4].ToString().ShouldBe("1356");
        simplified.Cells[5,5].ToString().ShouldBe("1356");
        simplified.Cells[5,4].ToString().ShouldBe("16789");
        simplified.Cells[5,3].ToString().ShouldBe("16789");
        simplified.Cells[3,5].ToString().ShouldBe("16789");
        simplified.Cells[3,4].ToString().ShouldBe("16789");
    }
}