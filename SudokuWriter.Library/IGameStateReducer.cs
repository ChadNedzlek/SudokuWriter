namespace SudokuWriter.Library;

public interface IGameStateReducer
{
    GameState? TryReduce(GameState state);
}