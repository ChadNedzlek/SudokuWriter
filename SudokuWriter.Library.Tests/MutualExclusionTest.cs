using System;
using NUnit.Framework;
using Shouldly;

namespace SudokuWriter.Library.Tests;

[TestFixture]
[TestOf(typeof(MutualExclusion))]
public class MutualExclusionTest
{
    [Test]
    public void BasicDouble()
    {
        Span<ushort> cells = [3, 3, 511, 511, 511, 511, 511, 511, 511];
        var refs = new MultiRef<ushort>(cells);
        refs.IncludeStride(0, (ushort)cells.Length);
        MutualExclusion.ApplyMutualExclusionRules(refs).ShouldBe(true);
        cells.SequenceEqual<ushort>([3,3,508,508,508,508,508,508,508]).ShouldBeTrue();
    }

    [Test]
    public void HiddenTriple()
    {
        Span<ushort> cells = [3, 5, 6, 511, 511, 511, 511, 511, 511];
        var refs = new MultiRef<ushort>(cells);
        refs.IncludeStride(0, (ushort)cells.Length);
        MutualExclusion.ApplyMutualExclusionRules(refs).ShouldBe(true);
        cells.SequenceEqual<ushort>([3,5,6,504,504,504,504,504,504]).ShouldBeTrue();
    }
}