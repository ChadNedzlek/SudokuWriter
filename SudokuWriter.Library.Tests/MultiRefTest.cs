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
    
    [Test]
    public void IsStrictSuperSet_ValidSuperSet_ReturnsTrue()
    {
        Span<int> values = stackalloc int[8];
        var a = new MultiRef<int>(values);
        a.Include(ref values[1]);
        a.Include(ref values[2]);
        a.Include(ref values[3]);
        
        var b = new MultiRef<int>(values);
        b.Include(ref values[1]);
        b.Include(ref values[2]);
        
        a.IsStrictSuperSetOf(b).ShouldBeTrue();
    }
    
    [Test]
    public void IsStrictSuperSet_DisjointSet_ReturnsFalse()
    {
        Span<int> values = stackalloc int[8];
        var a = new MultiRef<int>(values);
        a.Include(ref values[1]);
        a.Include(ref values[2]);
        a.Include(ref values[3]);
        
        var b = new MultiRef<int>(values);
        b.Include(ref values[6]);
        b.Include(ref values[7]);
        
        a.IsStrictSuperSetOf(b).ShouldBeFalse();
    }
    
    [Test]
    public void IsStrictSuperSet_SameSet_ReturnsFalse()
    {
        Span<int> values = stackalloc int[8];
        var a = new MultiRef<int>(values);
        a.Include(ref values[1]);
        a.Include(ref values[2]);
        a.Include(ref values[3]);
        MultiRef<int> b = a;
        
        a.IsStrictSuperSetOf(b).ShouldBeFalse();
    }
    
    [Test]
    public void ExceptSet()
    {
        Span<int> values = stackalloc int[20];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = i;
        }

        var a = new MultiRef<int>(values);
        a.IncludeStrides(0, 9, 0, 1);
        var b = new MultiRef<int>(values);
        b.Include(ref values[1]);
        b.Include(ref values[2]);

        a.Except(b);

        Span<ushort> offsets = stackalloc ushort[MultiRef<int>.MaxCount];
        offsets = offsets[..a.GetOffsets(offsets)];
        offsets.Length.ShouldBe(7);
        offsets.ShouldBe<ushort>([0, 3, 4, 5, 6, 7, 8], ignoreOrder: true);
    }
}