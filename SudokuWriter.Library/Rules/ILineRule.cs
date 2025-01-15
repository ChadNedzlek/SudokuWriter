using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace SudokuWriter.Library.Rules;

public interface ILineRule<T> : IGameRule
{
    static abstract IGameRule Create(ImmutableList<LineRuleSegment> parts, JsonObject jsonObject);
}