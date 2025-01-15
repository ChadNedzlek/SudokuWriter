using System;
using System.Text.Json.Nodes;

namespace SudokuWriter.Library.Rules;

[GameRule("kropki")]
public class KropkiDotRule : IGameRule
{
    public GameResult Evaluate(GameState state)
    {
        throw new NotImplementedException();
    }

    public GameState? TryReduce(GameState state)
    {
        throw new NotImplementedException();
    }

    public JsonObject ToJsonObject()
    {
        throw new NotImplementedException();
    }

    public static IGameRule FromJsonObject(JsonObject jsonObject)
    {
        throw new NotImplementedException();
    }
}