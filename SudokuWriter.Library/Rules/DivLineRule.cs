using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace VaettirNet.SudokuWriter.Library.Rules;

[GameRule("div-line")]
public class DivLineRule : TriGroupLineRule<DivLineRule>, ILineRule<DivLineRule>
{
    public DivLineRule(ImmutableArray<BranchingRuleLine> lines) : base(lines)
    {
    }

    public static IGameRule Create(ImmutableArray<BranchingRuleLine> parts, JsonObject jsonObject) => new DivLineRule(parts);

    protected override CellValueMask ReduceToGroup(CellValueMask input) => input | (input >> 1) | (input >> 2);
    protected override CellValueMask ReducingGroupMask(CellValueMask mask) => mask | mask << 1 | mask << 2;

    protected override CellValueMask InputGroupMask { get; } = new CellValue(0) | new CellValue(3) | new CellValue(6);

}