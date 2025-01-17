using System;
using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace SudokuWriter.Library.Rules;

[GameRule("renban")]
public class RenbanLine : LineRule<RenbanLine>, ILineRule<RenbanLine>
{
    public RenbanLine(ImmutableArray<BranchingRuleLine> segments) : base(segments)
    {
    }
    
    public static IGameRule Create(ImmutableArray<BranchingRuleLine> parts, JsonObject jsonObject) => new RenbanLine(parts);
    
    public override GameResult Evaluate(GameState state)
    {
        throw new NotImplementedException();
    }

    public override GameState? TryReduce(GameState state)
    {
        throw new NotImplementedException();
    }

    public override void SaveToJsonObject(JsonObject obj)
    {
    }
}