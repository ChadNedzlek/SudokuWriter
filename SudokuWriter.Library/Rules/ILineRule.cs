using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace VaettirNet.SudokuWriter.Library.Rules;

public interface ILineRule<T> : IGameRule
{
    static abstract T Create(ImmutableArray<BranchingRuleLine> parts, JsonObject jsonObject);
}