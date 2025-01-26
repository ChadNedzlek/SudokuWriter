using System.Collections.Generic;

namespace SudokuWriter.Library;

public interface IGameStateReducer
{
    GameState? TryReduce(GameState state);

    IEnumerable<MultiRefBox<ushort>> GetMutualExclusionGroups(GameState state);
}