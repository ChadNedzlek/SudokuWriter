using System;

namespace SudokuWriter.Library;

public class GameRuleAttribute : Attribute
{
    public GameRuleAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}