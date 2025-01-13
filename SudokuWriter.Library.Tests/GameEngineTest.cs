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
        var state = new GameState(
            Cells.CreateFilled(4, 4, 4),
            2,
            2,
            4
        );
        var sw = Stopwatch.StartNew();
        GameEngine.Default.Evaluate(state, out _, out _).Should().Be(GameResult.MultipleSolutions);
        TestContext.WriteLine($"Completed in {sw.Elapsed}");
    }

    [Test]
    public void AmbiguousNormalGame()
    {
        var state = new GameState(Cells.CreateFilled());
        var sw = Stopwatch.StartNew();
        GameEngine.Default.Evaluate(state, out _, out _).Should().Be(GameResult.MultipleSolutions);
        TestContext.WriteLine($"Completed in {sw.Elapsed}");
    }

    [Test]
    public void SolvableTinyNormalGame()
    {
        var state = new GameState(
            Cells.CreateFilled(2, 2, 2).SetCell(0, 0, 0),
            1,
            2,
            2
        );
        var sw = Stopwatch.StartNew();
        GameEngine.Default.Evaluate(state, out GameState? solved, out _).Should().Be(GameResult.Solved);
        TestContext.WriteLine($"Completed in {sw.Elapsed}");
        solved.Value.Cells.GetSingle(0, 0).Should().Be(0);
        solved.Value.Cells.GetSingle(0, 1).Should().Be(1);
        solved.Value.Cells.GetSingle(1, 0).Should().Be(1);
        solved.Value.Cells.GetSingle(1, 1).Should().Be(0);
    }
}