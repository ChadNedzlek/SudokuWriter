using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;

namespace SudokuWriter.Library;

public class GameEngine
{
    private GameEngine(IEnumerable<IGameRule> rules) : this(rules.ToImmutableArray())
    {
    }

    private GameEngine(params ImmutableArray<IGameRule> rules)
    {
        Rules = rules;
    }

    public static GameEngine NoRules { get; } = new();
    public static GameEngine Default { get; } = new(BasicGameRule.Instance);

    public ImmutableArray<IGameRule> Rules { get; }

    public GameEngine AddRule(IGameRule rule)
    {
        return new GameEngine([..Rules, rule]);
    }

    public GameResult Evaluate(GameState state, out GameState? solution, out GameState? conflict)
    {
        PriorityQueue<GameState, int> searchStates = new();
        searchStates.Enqueue(state, GetPossibilities(state));
        HashSet<ulong> seenStates = [state.GetStateHash()];

        GameState? solved = null;
        while (searchStates.TryDequeue(out GameState s, out _))
            foreach (GameState next in NextStates(s))
            {
                GameResult result = Rules.Aggregate(
                    GameResult.Unknown,
                    (res, rule) => res switch
                    {
                        GameResult.Unsolvable => GameResult.Unsolvable,
                        _ => rule.Evaluate(next)
                    }
                );

                if (result == GameResult.Unsolvable)
                {
                    continue;
                }

                GameState simplified = next;
                var reduced = true;
                while (reduced)
                {
                    reduced = false;
                    foreach (IGameRule rule in Rules)
                        if (rule.TryReduce(simplified) is { } simpleReduce)
                        {
                            simplified = simpleReduce;
                            reduced = true;
                            break;
                        }
                }

                GameResult simplifiedResult = Rules.Aggregate(
                    GameResult.Unknown,
                    (res, rule) => res switch
                    {
                        GameResult.Unsolvable => GameResult.Unsolvable,
                        _ => rule.Evaluate(simplified)
                    }
                );

                switch (simplifiedResult)
                {
                    case GameResult.Unsolvable:
                        continue;
                    case GameResult.Solved when solved.HasValue:
                        solution = solved;
                        conflict = simplified;
                        return GameResult.MultipleSolutions;
                    case GameResult.Solved:
                        solved = simplified;
                        continue;
                }

                if (!seenStates.Add(simplified.GetStateHash()))
                {
                    continue;
                }

                searchStates.Enqueue(simplified, GetPossibilities(simplified));
            }

        if (solved.HasValue)
        {
            solution = solved;
            conflict = null;
            return GameResult.Solved;
        }

        solution = null;
        conflict = null;
        return GameResult.Unsolvable;
    }

    public IEnumerable<GameState> NextStates(GameState initial)
    {
        int nRows = initial.Cells.Rows;
        int nColumns = initial.Cells.Columns;
        int nDigits = initial.Digits;

        int minPossibilities = initial.Digits + 1;
        var minRow = 0;
        var minColumn = 0;
        ushort minMask = 0;

        for (var r = 0; r < nRows; r++)
        for (var c = 0; c < nColumns; c++)
        {
            ushort mask = initial.Cells.GetMask(r, c);
            int bitCount = BitOperations.PopCount(mask);
            if (bitCount > 1 && bitCount < minPossibilities)
            {
                minPossibilities = bitCount;
                minRow = r;
                minColumn = c;
                minMask = mask;
            }
        }

        for (var d = 0; d < nDigits; d++)
        {
            if (!Cells.IsDigitSet(minMask, d))
            {
                continue;
            }

            yield return initial.WithCells(initial.Cells.SetCell(minRow, minColumn, d));
        }
    }

    public int GetPossibilities(GameState state)
    {
        int nRows = state.Cells.Rows;
        int nColumns = state.Cells.Columns;
        var possibilities = 0;
        for (var r = 0; r < nRows; r++)
        for (var c = 0; c < nColumns; c++)
            possibilities += BitOperations.PopCount(state.Cells.GetMask(r, c));

        return possibilities;
    }
}