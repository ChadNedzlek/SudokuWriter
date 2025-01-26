using System.Diagnostics;
using NUnit.Framework;
using Shouldly;

namespace VaettirNet.SudokuWriter.Library.Tests;

[TestFixture]
[TestOf(typeof(GameEngine))]
public class GameEngineTest
{
    [Test]
    public void AmbiguousTinyGame()
    {
        var s = new GameStructure(4, 4, 4, 2, 2);
        var state = new GameState(Cells.CreateFilled(s), s);
        var sw = Stopwatch.StartNew();
        GameEngine.Default.Evaluate(state, out _, out _).ShouldBe(GameResult.MultipleSolutions);
        TestContext.WriteLine($"Completed in {sw.Elapsed}");
    }

    [Test]
    public void AmbiguousNormalGame()
    {
        var state = new GameState(Cells.CreateFilled(), GameStructure.Default);
        var sw = Stopwatch.StartNew();
        GameEngine.Default.Evaluate(state, out _, out _).ShouldBe(GameResult.MultipleSolutions);
        TestContext.WriteLine($"Completed in {sw.Elapsed}");
    }

    [Test]
    public void SolvableTinyNormalGame()
    {
        var s = new GameStructure(2, 2, 2, 1, 2);
        var state = new GameState(Cells.CreateFilled(s).SetCell(0, 0, 0), s);
        var sw = Stopwatch.StartNew();
        GameEngine.Default.Evaluate(state, out GameState? solved, out _).ShouldBe(GameResult.Solved);
        TestContext.WriteLine($"Completed in {sw.Elapsed}");
        solved.ShouldNotBeNull();
        solved.Value.Cells.GetSingle(0, 0).ShouldBe(0);
        solved.Value.Cells.GetSingle(0, 1).ShouldBe(1);
        solved.Value.Cells.GetSingle(1, 0).ShouldBe(1);
        solved.Value.Cells.GetSingle(1, 1).ShouldBe(0);
    }
}