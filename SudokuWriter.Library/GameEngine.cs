using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Library;

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

    public GameResult Evaluate(
        GameState state,
        out GameState? solution,
        out GameState? conflict,
        CancellationToken cancellationToken = default
    )
    => Evaluate(state, out solution, out conflict, NoopTracker.Instance, out _, cancellationToken);

    public GameResult Evaluate(
        GameState state,
        out GameState? solution,
        out GameState? conflict,
        ISimplificationTracker tracker,
        out ISimplificationChain solutionChain,
        CancellationToken cancellationToken = default)
    {
        solutionChain = tracker.GetEmptyChain();
        state = SimplifyState(state, solutionChain, cancellationToken);
        GameResult initialState = Rules.Aggregate(
            GameResult.Solved,
            (res, rule) => res switch
            {
                GameResult.Unsolvable => GameResult.Unsolvable,
                GameResult.Unknown => rule.Evaluate(state) switch
                {
                    GameResult.Unsolvable => GameResult.Unsolvable,
                    _ => GameResult.Unknown
                },
                _ => rule.Evaluate(state)
            }
        );

        if (initialState != GameResult.Unknown)
        {
            solution = state;
            conflict = null;
            return initialState;
        }

        PriorityQueue<GameState, int> searchStates = new();
        searchStates.Enqueue(state, GetPossibilities(state));
        HashSet<ulong> seenStates = [state.GetStateHash()];

        GameState? solved = null;
        while (searchStates.TryDequeue(out GameState s, out _))
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (GameState next in NextStates(s))
            {
                GameResult result = EvaluateState(next);

                if (result == GameResult.Unsolvable) continue;

                GameState simplified = SimplifyState(next, solutionChain, cancellationToken);

                switch (EvaluateState(simplified))
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

    private GameResult EvaluateState(GameState state)
    {
        return Rules.Aggregate(
            GameResult.Solved,
            (res, rule) => res switch
            {
                GameResult.Unsolvable => GameResult.Unsolvable,
                GameResult.Unknown => rule.Evaluate(state) switch
                {
                    GameResult.Unsolvable => GameResult.Unsolvable,
                    _ => GameResult.Unknown
                },
                _ => rule.Evaluate(state)
            }
        );
    }

    public GameState SimplifyState(GameState next, ISimplificationChain chain = null, CancellationToken cancellationToken = default)
    {
        chain ??= NoopTracker.Instance.GetEmptyChain();
        GameState simplified = next;
        bool reduced = true;
        while (reduced)
        {
            cancellationToken.ThrowIfCancellationRequested();
            reduced = false;
            foreach (IGameRule rule in Rules)
            {
                if (rule.TryReduce(simplified, chain) is { } simpleReduce)
                {
                    simplified = simpleReduce;
                    reduced = true;
                    break;
                }
            }

            if (!reduced)
            {
                CellsBuilder cellBuilder = simplified.Cells.ToBuilder();
                foreach (IGameRule rule in Rules)
                foreach (MutexGroup mutexGroup in rule.GetMutualExclusionGroups(simplified, chain.Tracker))
                {
                    MultiRef<CellValueMask> cellRef = cellBuilder.Unbox(mutexGroup.Cells);
                    bool applied = MutualExclusion.ApplyMutualExclusionRules(cellRef);
                    if (applied)
                    {
                        chain.Record($"{mutexGroup.SimplificationRecord.Description} became {cellRef}");
                    }
                    reduced |= applied;
                }
                if (reduced) simplified = simplified.WithCells(cellBuilder.MoveToImmutable());
            }
            
            if (!reduced)
            {
                CellsBuilder cellBuilder = simplified.Cells.ToBuilder();
                List<DigitFence> fencedDigits = Rules.SelectMany(r => r.GetFencedDigits(simplified, chain.Tracker).ToList()).ToList();
                foreach (IGameRule rule in Rules)
                foreach (MutexGroup mutexGroup in rule.GetMutualExclusionGroups(simplified, chain.Tracker))
                {
                    if (EvaluateFenceLimitations(cellBuilder, mutexGroup, fencedDigits, chain))
                    {
                        // Evaluating a fence might screw up other fences, so we need to bail now, unfortunately
                        reduced = true;
                        break;
                    }
                }

                if (reduced) simplified = simplified.WithCells(cellBuilder.MoveToImmutable());
            }
        }

        return simplified;
    }

    private static bool EvaluateFenceLimitations(
        CellsBuilder cellBuilder,
        MutexGroup mutexGroup,
        List<DigitFence> fencedDigits,
        ISimplificationChain chain
    )
    {
        MultiRef<CellValueMask> mutexRef = cellBuilder.Unbox(mutexGroup.Cells);
        foreach (DigitFence digitFence in fencedDigits)
        {
            if (!mutexGroup.Cells.IsStrictSuperSetOf(digitFence.Cells))
                continue;
            
            MultiRef<CellValueMask> modificationGroup = mutexRef;
            modificationGroup.Except(digitFence.Cells);

            if (modificationGroup.Aggregate(false, RuleHelpers.TryMask, ~digitFence.Digit.AsMask()))
            {
                // Evaluating a fence might screw up other fences, so we need to bail now, unfortunately
                chain.Record(
                    $"Digit must be in {digitFence.SimplificationRecord.Description}, so cannot be in {mutexGroup.SimplificationRecord.Description}"
                );
                return true;
            }
        }

        return false;
    }

    public IEnumerable<GameState> NextStates(GameState initial)
    {
        int nRows = initial.Cells.Rows;
        int nColumns = initial.Cells.Columns;
        int nDigits = initial.Digits;

        int minPossibilities = initial.Digits + 1;
        int minRow = 0;
        int minColumn = 0;
        CellValueMask minMask = CellValueMask.None;

        for (int r = 0; r < nRows; r++)
        for (int c = 0; c < nColumns; c++)
        {
            CellValueMask mask = initial.Cells[r, c];
            ushort bitCount = mask.Count;
            if (bitCount > 1 && bitCount < minPossibilities)
            {
                minPossibilities = bitCount;
                minRow = r;
                minColumn = c;
                minMask = mask;
            }
        }

        for (ushort d = 0; d < nDigits; d++)
        {
            var v = new CellValue(d);
            if (!minMask.Contains(v)) continue;

            yield return initial.WithCells(initial.Cells.SetCell(minRow, minColumn, v));
        }
    }

    public int GetPossibilities(GameState state)
    {
        int nRows = state.Cells.Rows;
        int nColumns = state.Cells.Columns;
        int possibilities = 0;
        for (int r = 0; r < nRows; r++)
        for (int c = 0; c < nColumns; c++)
            possibilities += state.Cells[r, c].Count;

        return possibilities;
    }

    public GameEngine WithRules(ImmutableArray<IGameRule> rules)
    {
        return new GameEngine(InitialState, rules);
    }
}