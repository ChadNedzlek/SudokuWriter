using System;
using JetBrains.Annotations;

namespace SudokuWriter.Library;

[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
public class GameRuleAttribute : Attribute
{
    public GameRuleAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}