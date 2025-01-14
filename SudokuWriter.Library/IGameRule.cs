using System.Text.Json;

namespace SudokuWriter.Library;

public interface IGameRule : IGameStateEvaluator, IGameStateReducer
{
    void WriteRule(Utf8JsonWriter writer);
}