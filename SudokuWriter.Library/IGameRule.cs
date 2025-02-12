using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace VaettirNet.SudokuWriter.Library;

public interface IGameRule
{
    JsonObject ToJsonObject();

    static virtual IGameRule FromJsonObject(JsonObject jsonObject)
    {
        throw new NotSupportedException();
    }

    GameResult Evaluate(GameState state);
    
    GameState? TryReduce(GameState state, ISimplificationChain chain);

    IEnumerable<MutexGroup> GetMutualExclusionGroups(GameState state, ISimplificationTracker tracker);

    IEnumerable<DigitFence> GetFencedDigits(GameState state, ISimplificationTracker tracker);
}

public readonly record struct MutexGroup(MultiRefBox<CellValueMask> Cells, SimplificationRecord SimplificationRecord);