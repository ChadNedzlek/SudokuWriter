using Shouldly;

namespace VaettirNet.SudokuWriter.Library.Tests;

public static class AssertExtensions
{
    public static void ShouldBe(
        this ushort actual,
        int expected,
        string customMessage = null)
    {
        ((int)actual).ShouldBe(expected, customMessage);
    }
    
    public static void ShouldBe(
        this CellValueMask actual,
        int expected,
        string customMessage = null)
    {
        actual.RawValue.ShouldBe(expected, customMessage);
    }
    
    public static void ShouldBe(
        this CellValue actual,
        int expected,
        string customMessage = null)
    {
        (actual.NumericValue - 1).ShouldBe(expected, customMessage);
    }
}