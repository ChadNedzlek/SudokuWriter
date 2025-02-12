using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace VaettirNet.SudokuWriter.Library.Rules;

[GameRule("mod-line")]
public class ModLineRule : TriGroupLineRule<ModLineRule>, ILineRule<ModLineRule>
{
    public ModLineRule(params ImmutableArray<BranchingRuleLine> lines) : base(lines)
    {
    }

    public static IGameRule Create(ImmutableArray<BranchingRuleLine> parts, JsonObject jsonObject) => new ModLineRule(parts);

    protected override CellValueMask ReduceToGroup(CellValueMask input) => input | (input >> 3) | (input >> 6);
    protected override CellValueMask ReducingGroupMask(CellValueMask mask) => mask | mask << 3 | mask << 6;
    protected override CellValueMask InputGroupMask { get; } = CellValueMask.All(3);
    public override IEnumerable<DigitFence> GetFencedDigits(GameState state, ISimplificationTracker tracker) => [];
}