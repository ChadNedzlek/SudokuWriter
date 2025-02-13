namespace VaettirNet.SudokuWriter.Library;

public readonly record struct SimplificationRecord(string Description)
{
    public static  SimplificationRecord Empty = new();
}