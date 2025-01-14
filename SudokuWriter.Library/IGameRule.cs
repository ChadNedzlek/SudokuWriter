using System;
using System.Text.Json.Nodes;

namespace SudokuWriter.Library;

public interface IGameRule : IGameStateEvaluator, IGameStateReducer
{
    JsonObject ToJsonObject();

    static virtual IGameRule FromJsonObject(JsonObject jsonObject)
    {
        throw new NotSupportedException();
    }
}