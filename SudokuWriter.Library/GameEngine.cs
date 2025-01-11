using System.Collections.Generic;
using System.Collections.Immutable;

namespace SudokuWriter.Library;

public class GameEngine
{
    public static GameEngine NoRules { get; } = new();
    public static GameEngine Default { get; } = new(BasicGameRule.Instance);

    private GameEngine(IEnumerable<IGameRule> rules) : this(rules.ToImmutableArray())
    {
    }

    private GameEngine(params ImmutableArray<IGameRule> rules)
    {
        Rules = rules;
    }

    public ImmutableArray<IGameRule> Rules { get; }

    public GameEngine AddRule(IGameRule rule) => new([..Rules, rule]);
}