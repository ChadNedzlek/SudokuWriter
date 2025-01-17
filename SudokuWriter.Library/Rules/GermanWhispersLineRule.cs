using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace SudokuWriter.Library.Rules;

[GameRule("german-whisper")]
public class GermanWhispersLineRule : DifferenceAtLeastLineRuleBase<GermanWhispersLineRule>, ILineRule<GermanWhispersLineRule>
{
    public GermanWhispersLineRule(ImmutableArray<BranchingRuleLine> segments) : base(segments, 5)
    {
    }
    
    public static IGameRule Create(ImmutableArray<BranchingRuleLine> parts, JsonObject jsonObject) => new GermanWhispersLineRule(parts);
    public override void SaveToJsonObject(JsonObject obj)
    {
    }
}