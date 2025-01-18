using System.Collections.Immutable;
using SudokuWriter.Library.CellAdjacencies;

namespace SudokuWriter.Library;

public interface IGameStateReducer
{
    GameState? TryReduce(GameState state);
    // ImmutableList<ReadOnlyCellGroup> GetBoundGroups(GameState state);
}