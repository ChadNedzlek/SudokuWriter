namespace SudokuWriter.Library;

public interface IGameStateReducer
{
    bool TryReduce(ref GameState state);
}