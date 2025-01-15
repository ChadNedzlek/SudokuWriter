using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace SudokuWriter.Library.Rules;

[GameRule("german-whisper")]
public class GermanWhispersLineRule : DifferenceAtLeastLineRule<GermanWhispersLineRule>, ILineRule<GermanWhispersLineRule>
{
    public GermanWhispersLineRule(ImmutableList<LineRuleSegment> segments) : base(segments, 5)
    {
    }
    
    public new static IGameRule Create(ImmutableList<LineRuleSegment> parts, JsonObject jsonObject) => new GermanWhispersLineRule(parts);
}