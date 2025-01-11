using System;
using FluentAssertions;
using NUnit.Framework;

namespace SudokuWriter.Library.Tests;

[TestFixture]
[TestOf(typeof(ImmutableArray2))]
public class ImmutableArray2Test
{
    [Test]
    public void DefaultZeros()
    {
        var cells = ImmutableArray2.Create<int>(2, 2);
        cells[0, 0].Should().Be(0);
        cells[0, 1].Should().Be(0);
        cells[1, 0].Should().Be(0);
        cells[1, 1].Should().Be(0);
    }
    
    [Test]
    public void OutOfBoundsThrows()
    {
        var cells = ImmutableArray2.Create<int>(2, 2);
        ((Func<int>)(() => cells[2, 0])).Should().Throw<ArgumentOutOfRangeException>();
        ((Func<int>)(() => cells[0, 2])).Should().Throw<ArgumentOutOfRangeException>();
    }
    
    [Test]
    public void NegativeOrZeroBoundsThrow()
    {
        ((Func<object>)(() => ImmutableArray2.Create<int>(0, 2))).Should().Throw<ArgumentOutOfRangeException>();
        ((Func<object>)(() => ImmutableArray2.Create<int>(2, 0))).Should().Throw<ArgumentOutOfRangeException>();
        ((Func<object>)(() => ImmutableArray2.Create<int>(-1, 2))).Should().Throw<ArgumentOutOfRangeException>();
        ((Func<object>)(() => ImmutableArray2.Create<int>(2, -1))).Should().Throw<ArgumentOutOfRangeException>();
    }
    
    [Test]
    public void SetChangesCell()
    {
        var cells = ImmutableArray2.Create<int>(2, 2).SetItem(1, 1, 5);
        cells[0, 0].Should().Be(0);
        cells[0, 1].Should().Be(0);
        cells[1, 0].Should().Be(0);
        cells[1, 1].Should().Be(5);
    }
    
    [Test]
    public void SetMultipleValues()
    {
        var cells = ImmutableArray2.Create<int>(2, 2).ToBuilder();
        cells[0, 1] = 5;
        cells[1, 1] = 50;
        cells[0, 0].Should().Be(0);
        cells[0, 1].Should().Be(5);
        cells[1, 0].Should().Be(0);
        cells[1, 1].Should().Be(50);
    }
}