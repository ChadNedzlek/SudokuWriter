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
}