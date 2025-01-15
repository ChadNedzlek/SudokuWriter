using System;
using NUnit.Framework;
using Shouldly;

namespace SudokuWriter.Library.Tests;

[TestFixture]
[TestOf(typeof(ImmutableArray2))]
public class ImmutableArray2Test
{
    [Test]
    public void DefaultZeros()
    {
        var cells = ImmutableArray2.Create<int>(2, 2);
        cells[0, 0].ShouldBe(0);
        cells[0, 1].ShouldBe(0);
        cells[1, 0].ShouldBe(0);
        cells[1, 1].ShouldBe(0);
    }

    [Test]
    public void OutOfBoundsThrows()
    {
        var cells = ImmutableArray2.Create<int>(2, 2);
        Should.Throw<ArgumentOutOfRangeException>(() => cells[2, 0]);
        Should.Throw<ArgumentOutOfRangeException>(() => cells[0, 2]);
    }

    [Test]
    public void NegativeOrZeroBoundsThrow()
    {
        ((Func<object>)(() => ImmutableArray2.Create<int>(0, 2))).ShouldThrow<ArgumentOutOfRangeException>();
        ((Func<object>)(() => ImmutableArray2.Create<int>(2, 0))).ShouldThrow<ArgumentOutOfRangeException>();
        ((Func<object>)(() => ImmutableArray2.Create<int>(-1, 2))).ShouldThrow<ArgumentOutOfRangeException>();
        ((Func<object>)(() => ImmutableArray2.Create<int>(2, -1))).ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Test]
    public void SetChangesCell()
    {
        ImmutableArray2<int> cells = ImmutableArray2.Create<int>(2, 2).SetItem(1, 1, 5);
        cells[0, 0].ShouldBe(0);
        cells[0, 1].ShouldBe(0);
        cells[1, 0].ShouldBe(0);
        cells[1, 1].ShouldBe(5);
    }

    [Test]
    public void SetMultipleValues()
    {
        ImmutableArray2.Builder<int> cells = ImmutableArray2.Create<int>(2, 2).ToBuilder();
        cells[0, 1] = 5;
        cells[1, 1] = 50;
        cells[0, 0].ShouldBe(0);
        cells[0, 1].ShouldBe(5);
        cells[1, 0].ShouldBe(0);
        cells[1, 1].ShouldBe(50);
    }
}