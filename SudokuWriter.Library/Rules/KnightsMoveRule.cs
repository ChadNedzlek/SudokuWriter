using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace SudokuWriter.Library.Rules;

[GameRule("knight")]
public class KnightsMoveRule : IGameRule
{
    private static readonly ImmutableArray<GridCoord> s_knightOffsets =
    [
        // We only need half the circle, because every pair goes both ways
        (2, 1), (1, 2), (-1, 2), (-2, 1)
    ];  
    
    public GameResult Evaluate(GameState state)
    {
        GameResult current = GameResult.Solved;
        for (int r = 0; r < state.Structure.Rows; r++)
        for (int c = 0; c < state.Structure.Columns; c++)
        {
            int single = state.Cells.GetSingle(r, c);
            if (single == -1)
            {
                continue;
            }

            foreach (var ko in s_knightOffsets)
            {
                int ro = r + ko.Row;
                int co = c + ko.Col;
                
                if (ro < 0 || ro >= state.Structure.Rows) continue;
                if (co < 0 || co >= state.Structure.Columns) continue;

                int knightSingle = state.Cells.GetSingle(ro, co);
                if (knightSingle == single)
                {
                    return GameResult.Unsolvable;
                }

                if (knightSingle == -1)
                {
                    current = GameResult.Unknown;
                }
            }
        }

        return current;
    }

    public GameState? TryReduce(GameState state) => null;

    public JsonObject ToJsonObject() => new();

    public static IGameRule FromJsonObject(JsonObject jsonObject) => new KnightsMoveRule();
}