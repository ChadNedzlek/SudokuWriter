using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;

namespace SudokuWriter.Library.Tests;

[TestFixture]
[TestOf(typeof(GameEngine))]
public class GameEngineTest
{
    [Test]
    public void AmbiguousTinyGame()
    {
        GameStructure s = new GameStructure(4, 4, 4, 2, 2);
        var state = new GameState(Cells.CreateFilled(s), s);
        var sw = Stopwatch.StartNew();
        GameEngine.Default.Evaluate(state, out _, out _).Should().Be(GameResult.MultipleSolutions);
        TestContext.WriteLine($"Completed in {sw.Elapsed}");
    }

    [Test]
    public void AmbiguousNormalGame()
    {
        var state = new GameState(Cells.CreateFilled(), GameStructure.Default);
        var sw = Stopwatch.StartNew();
        GameEngine.Default.Evaluate(state, out _, out _).Should().Be(GameResult.MultipleSolutions);
        TestContext.WriteLine($"Completed in {sw.Elapsed}");
    }

    [Test]
    public void SolvableTinyNormalGame()
    {
        GameStructure s = new GameStructure(2, 2, 2, 1, 2);
        var state = new GameState(Cells.CreateFilled(s).SetCell(0, 0, 0), s);
        var sw = Stopwatch.StartNew();
        GameEngine.Default.Evaluate(state, out GameState? solved, out _).Should().Be(GameResult.Solved);
        TestContext.WriteLine($"Completed in {sw.Elapsed}");
        solved.Value.Cells.GetSingle(0, 0).Should().Be(0);
        solved.Value.Cells.GetSingle(0, 1).Should().Be(1);
        solved.Value.Cells.GetSingle(1, 0).Should().Be(1);
        solved.Value.Cells.GetSingle(1, 1).Should().Be(0);
    }
}