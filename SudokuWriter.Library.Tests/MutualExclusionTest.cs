using System;
using NUnit.Framework;
using Shouldly;

namespace VaettirNet.SudokuWriter.Library.Tests;

[TestFixture]
[TestOf(typeof(MutualExclusion))]
public class MutualExclusionTest
{
    [Test]
    public void BasicDouble()
    {
        CellValueMask low = new CellValue(0) | new CellValue(1);
        CellValueMask all = CellValueMask.All(9);
        CellValueMask excluded = all & ~low;
        Span<CellValueMask> cells = [low, low, all, all, all, all, all, all, all];
        var refs = new MultiRef<CellValueMask>(cells);
        refs.IncludeStride(0, (ushort)cells.Length);
        MutualExclusion.ApplyMutualExclusionRules(refs).ShouldBe(true);
        cells.SequenceEqual([low, low, excluded, excluded, excluded, excluded, excluded, excluded, excluded]).ShouldBeTrue();
    }

    [Test]
    public void HiddenTriple()
    {
        CellValueMask double12 = new CellValue(0) | new CellValue(1);
        CellValueMask double23 = new CellValue(2) | new CellValue(1);
        CellValueMask double13 = new CellValue(0) | new CellValue(2);
        CellValueMask all = CellValueMask.All(9);
        CellValueMask excluded = all & ~(double12 | double23 | double13);
        Span<CellValueMask> cells = [double12, double23, double13, all, all, all, all, all, all];
        var refs = new MultiRef<CellValueMask>(cells);
        refs.IncludeStride(0, (ushort)cells.Length);
        MutualExclusion.ApplyMutualExclusionRules(refs).ShouldBe(true);
        cells.SequenceEqual([double12, double23, double13, excluded, excluded, excluded, excluded, excluded, excluded]).ShouldBeTrue();
    }
}