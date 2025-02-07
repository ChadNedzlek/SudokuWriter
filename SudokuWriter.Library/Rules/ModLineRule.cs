using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using VaettirNet.SudokuWriter.Library.CellAdjacencies;

namespace VaettirNet.SudokuWriter.Library.Rules;

[GameRule("mod-line")]
public class ModLineRule : TriLineRule<ModLineRule>, ILineRule<ModLineRule>
{
    public ModLineRule(ImmutableArray<BranchingRuleLine> lines) : base(lines)
    {
    }

    public override void SaveToJsonObject(JsonObject obj)
    {
    }

    public static IGameRule Create(ImmutableArray<BranchingRuleLine> parts, JsonObject jsonObject) => new ModLineRule(parts);

    protected override GameResult EvaluateGroup(in ReadOnlyMultiRef<CellValueMask> a, in ReadOnlyMultiRef<CellValueMask> b, in ReadOnlyMultiRef<CellValueMask> c)
    {
        CellValueMask three = CellValueMask.All(3);
        var anyA = a.Aggregate(CellValueMask.None, (m, v) => m | (v | (v >> 3) | (v >> 6)) & three);
        var anyB = b.Aggregate(CellValueMask.None, (m, v) => m | (v | (v >> 3) | (v >> 6)) & three);
        var anyC = c.Aggregate(CellValueMask.None, (m, v) => m | (v | (v >> 3) | (v >> 6)) & three);
        
        bool cHasCells = !c.IsEmpty;
        if (cHasCells)
        {
            if ((anyA | anyB | anyC) != three) return GameResult.Unsolvable;
        }
        else
        {
            if ((anyA | anyB).Count < 2) return GameResult.Unsolvable;
        }

        var onlyA = a.Aggregate(three, (m, v) => m & (v | (v >> 3) | (v >> 6)));
        var onlyB = b.Aggregate(three, (m, v) => m & (v | (v >> 3) | (v >> 6)));
        var onlyC = c.Aggregate(three, (m, v) => m & (v | (v >> 3) | (v >> 6)));

        if (onlyA == CellValueMask.None || onlyB == CellValueMask.None || (onlyC == CellValueMask.None && cHasCells))
            return GameResult.Unsolvable;
        
        return onlyA.Count == 1 && onlyB.Count == 1 && (onlyC.Count == 1 || !cHasCells) ? GameResult.Solved : GameResult.Unknown;
    }

    protected override bool ReduceGroups(ref MultiRef<CellValueMask> a, ref MultiRef<CellValueMask> b, ref MultiRef<CellValueMask> c)
    {
        CellValueMask three = CellValueMask.All(3);
        bool reduced = false;
        
        var onlyA = a.Aggregate(three, (CellValueMask m, ref CellValueMask v) => m & (v | (v >> 3) | (v >> 6)));
        var onlyB = b.Aggregate(three, (CellValueMask m, ref CellValueMask v) => m & (v | (v >> 3) | (v >> 6)));
        var onlyC = c.Aggregate(three, (CellValueMask m, ref CellValueMask v) => m & (v | (v >> 3) | (v >> 6)));

        reduced |= MaskOtherCells(onlyA, ref b, ref c);
        reduced |= MaskOtherCells(onlyB, ref a, ref c);
        reduced |= MaskOtherCells(onlyC, ref a, ref b);
        reduced |= SelfMask(onlyA, ref a);
        reduced |= SelfMask(onlyB, ref b);
        reduced |= SelfMask(onlyC, ref c);

        return reduced;

        static CellValueMask ToMask(CellValueMask modMask) => modMask | modMask << 3 | modMask << 6;

        static bool SelfMask(CellValueMask src, ref MultiRef<CellValueMask> a)
        {
            var m = ToMask(src);
            return a.Aggregate(false, (bool r, ref CellValueMask v) => r | RuleHelpers.TryMask(ref v, m));
        }

        static bool MaskOtherCells(CellValueMask src, ref MultiRef<CellValueMask> b, ref MultiRef<CellValueMask> c)
        {
            if (src.Count != 1)
            {
                return false;
            }

            bool reduced = false;
            CellValueMask three = CellValueMask.All(3);
            var aMask = ToMask(three & ~src);
            reduced |= b.Aggregate(false, (bool r, ref CellValueMask v) => r | RuleHelpers.TryMask(ref v, aMask));
            reduced |= c.Aggregate(false, (bool r, ref CellValueMask v) => r | RuleHelpers.TryMask(ref v, aMask));

            return reduced;
        }
    }
}