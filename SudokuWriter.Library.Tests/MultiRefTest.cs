using System;
using NUnit.Framework;
using Shouldly;

namespace VaettirNet.SudokuWriter.Library.Tests;

[TestFixture]
[TestOf(typeof(MultiRef<>))]
public class MultiRefTest
{
    [Test]
    public void StackallocTest()
    {
        Span<int> values = stackalloc int[8];
        var refs = new MultiRef<int>(values);
        refs.Include(ref values[3]);
        refs.Include(ref values[5]);
        refs.ForEach((ref int r) => r = 55);
        
        values[3].ShouldBe(55);
        values[5].ShouldBe(55);
        
        values[0].ShouldBe(0);
        values[1].ShouldBe(0);
        values[2].ShouldBe(0);
        values[4].ShouldBe(0);
        values[6].ShouldBe(0);
        values[7].ShouldBe(0);
    }
}