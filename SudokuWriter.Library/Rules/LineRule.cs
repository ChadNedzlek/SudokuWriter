using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json.Nodes;
using VaettirNet.SudokuWriter.Library.CellAdjacencies;

namespace VaettirNet.SudokuWriter.Library.Rules;

public interface ILineRule
{
    ImmutableArray<BranchingRuleLine> Lines { get; }
}

public abstract class LineRule<T> : IGameRule, ILineRule where T:ILineRule<T>
{
    public ImmutableArray<BranchingRuleLine> Lines { get; }

    protected LineRule(ImmutableArray<BranchingRuleLine> lines)
    {
        Lines = lines;
    }

    public abstract GameResult Evaluate(GameState state);
    public abstract GameState? TryReduce(GameState state, ISimplificationChain chain);
    public abstract IEnumerable<MutexGroup> GetMutualExclusionGroups(GameState state, ISimplificationTracker tracker);
    public abstract IEnumerable<DigitFence> GetFencedDigits(GameState state, ISimplificationTracker tracker);

    public virtual void SaveToJsonObject(JsonObject obj)
    {
    }

    public JsonObject ToJsonObject()
    {
        JsonObject o = new();
        JsonArray lines = new();
        foreach (BranchingRuleLine segment in Lines)
        {
            JsonArray branches = new();
            foreach (LineRuleSegment branch in segment.Branches)
            {
                JsonArray cells = new JsonArray();
                foreach (var cell in branch.Cells)
                {
                    cells.Add(new JsonArray(cell.Row, cell.Col));
                }
                branches.Add(cells);
            }
            lines.Add(branches);
        }

        SaveToJsonObject(o);
        
        o["lines"] = lines;
        return o;
    }

    public static IGameRule FromJsonObject(JsonObject jsonObject)
    {
        if (jsonObject["lines"] is not JsonArray arr)
        {
            throw new InvalidDataException("Missing 'lines'");
        }

        var lines = ImmutableArray.CreateBuilder<BranchingRuleLine>();
        foreach (JsonNode lineNode in arr)
        {
            if (lineNode is not JsonArray lineArray)
            {
                throw new InvalidDataException("Invalid 'lines'");
            }

            var branches = ImmutableArray.CreateBuilder<LineRuleSegment>();
            foreach (JsonNode branchNode in lineArray)
            {
                if (branchNode is not JsonArray branchArray)
                {
                    throw new InvalidDataException("Invalid 'lines'");
                }

                var cells = ImmutableArray.CreateBuilder<GridCoord>();
                foreach (JsonNode cellNode in branchArray)
                {
                    if (cellNode is not JsonArray { Count: 2 } cellArray)
                    {
                        throw new InvalidDataException("Invalid 'lines'");
                    }

                    cells.Add(new GridCoord(cellArray[0].GetValue<ushort>(), cellArray[1].GetValue<ushort>()));
                }

                branches.Add(new LineRuleSegment(cells.ToImmutable()));
            }
            lines.Add(new BranchingRuleLine(branches.ToImmutable()));
        }

        return T.Create(lines.ToImmutable(), jsonObject);
    }

    public LineCellEnumerable GetLineAdjacencies(CellsBuilder cells) => new(cells, this);
    public ReadOnlyLineCellEnumerable GetLineAdjacencies(Cells cells) => new(cells, this);
}