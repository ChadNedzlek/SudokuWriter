using System.Collections.Generic;

namespace VaettirNet.SudokuWriter.Library;

public interface IGameStateReducer
{
    GameState? TryReduce(GameState state);

    IEnumerable<MultiRefBox<CellValueMask>> GetMutualExclusionGroups(GameState state);
}