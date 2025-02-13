using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace VaettirNet.SudokuWriter.Library.Rules;

[GameRule("german-whisper")]
public class GermanWhispersLineRule : DifferenceAtLeastLineRuleBase<GermanWhispersLineRule>, ILineRule<GermanWhispersLineRule>
{
    public GermanWhispersLineRule(ImmutableArray<BranchingRuleLine> lines) : base(lines, 5)
    {
    }
    
    public static GermanWhispersLineRule Create(ImmutableArray<BranchingRuleLine> parts, JsonObject jsonObject) => new GermanWhispersLineRule(parts);
}