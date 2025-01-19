using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using SudokuWriter.Library.Rules;

namespace SudokuWriter.Library;

public class GameEngine
{
    public GameEngine(GameState initialState, IEnumerable<IGameRule> rules) : this(initialState, rules.ToImmutableArray())
    {
    }

    public GameEngine(GameState initialState, params ImmutableArray<IGameRule> rules)
    {
        InitialState = initialState.WithCells(initialState.Cells.FillEmpty(initialState.Structure));
        Rules = rules;
    }

    public GameState InitialState { get; }
    public ImmutableArray<IGameRule> Rules { get; }

    public static GameEngine Default { get; } =
        new(
            new GameState(
                Cells.CreateFilled(GameStructure.Default),
                GameStructure.Default
            ),
            BasicGameRule.Instance
        );

    public GameEngine WithInitialState(GameState state)
    {
        return new GameEngine(state, Rules);
    }

    public GameEngine AddRule(IGameRule rule)
    {
        return new GameEngine(InitialState, [..Rules, rule]);
    }

    public GameResult Evaluate(GameState state, out GameState? solution, out GameState? conflict)
    {
        state = SimlifyState(state);
        
        PriorityQueue<GameState, int> searchStates = new();
        searchStates.Enqueue(state, GetPossibilities(state));
        HashSet<ulong> seenStates = [state.GetStateHash()];

        GameState? solved = null;
        while (searchStates.TryDequeue(out GameState s, out _))
            foreach (GameState next in NextStates(s))
            {
                GameResult result = Rules.Aggregate(
                    GameResult.Solved,
                    (res, rule) => res switch
                    {
                        GameResult.Unsolvable => GameResult.Unsolvable,
                        GameResult.Unknown => rule.Evaluate(next) switch
                        {
                            GameResult.Unsolvable => GameResult.Unsolvable,
                            _ => GameResult.Unknown
                        },
                        _ => rule.Evaluate(next),
                    }
                );

                if (result == GameResult.Unsolvable) continue;

                GameState simplified = SimlifyState(next);

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

                if (!seenStates.Add(simplified.GetStateHash())) continue;

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

    private GameState SimlifyState(GameState next)
    {
        GameState simplified = next;
        bool reduced = true;
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

        return simplified;
    }

    public IEnumerable<GameState> NextStates(GameState initial)
    {
        int nRows = initial.Cells.Rows;
        int nColumns = initial.Cells.Columns;
        int nDigits = initial.Digits;

        int minPossibilities = initial.Digits + 1;
        int minRow = 0;
        int minColumn = 0;
        ushort minMask = 0;

        for (int r = 0; r < nRows; r++)
        for (int c = 0; c < nColumns; c++)
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

        for (int d = 0; d < nDigits; d++)
        {
            if (!Cells.IsDigitSet(minMask, d)) continue;

            yield return initial.WithCells(initial.Cells.SetCell(minRow, minColumn, d));
        }
    }

    public int GetPossibilities(GameState state)
    {
        int nRows = state.Cells.Rows;
        int nColumns = state.Cells.Columns;
        int possibilities = 0;
        for (int r = 0; r < nRows; r++)
        for (int c = 0; c < nColumns; c++)
            possibilities += BitOperations.PopCount(state.Cells.GetMask(r, c));

        return possibilities;
    }

    public GameEngine WithRules(ImmutableArray<IGameRule> rules) => new(InitialState, rules);
}