namespace VaettirNet.SudokuWriter.Library;

public interface IGameStateEvaluator
{
    GameResult Evaluate(GameState state);
}