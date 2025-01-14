using System;

namespace SudokuWriter.Library;

public class GameRuleAttribute : Attribute
{
    public string Name { get; }

    public GameRuleAttribute(string name)
    {
        Name = name;
    }
}