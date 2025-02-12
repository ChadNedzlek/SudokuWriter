using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace VaettirNet.SudokuWriter.Library.Rules;

[GameRule("quad")]
public class QuadRule : IGameRule
{
    public readonly record struct Quad(GridCoord Coord, CellValue A, CellValue B, CellValue C, CellValue D);
    
    public QuadRule(ImmutableArray<Quad> quads)
    {
        Quads = quads;
    }
    
    public ImmutableArray<Quad> Quads { get; }

    public GameResult Evaluate(GameState state)
    {
        foreach (Quad quad in Quads)
        {
            ReadOnlyMultiRef<CellValueMask> qRef = GetQuadRef(state.Cells, quad.Coord);
            if (!Matched(in qRef, quad.A)) return GameResult.Unsolvable;
            if (!Matched(in qRef, quad.B)) return GameResult.Unsolvable;
            if (!Matched(in qRef, quad.C)) return GameResult.Unsolvable;
            if (!Matched(in qRef, quad.D)) return GameResult.Unsolvable;
        }

        return GameResult.Solved;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool Matched(in ReadOnlyMultiRef<CellValueMask> r, CellValue v)
    {
        return v == CellValue.None || r.Aggregate(false, (m, c) => m || c.Contains(v));
    }

    private ReadOnlyMultiRef<CellValueMask> GetQuadRef(Cells cells, GridCoord coord)
    {
        var r = cells.GetEmptyReferences();
        r.Include(in cells[coord]);
        r.Include(in cells[coord + (0,1)]);
        r.Include(in cells[coord + (1,0)]);
        r.Include(in cells[coord + (1,1)]);
        return r;
    }

    public GameState? TryReduce(GameState state, ISimplificationChain chain)
    {
        // This rule is essentially _only_ a fence rule, so there's no reduction to be done.
        return null;
    }

    public IEnumerable<MutexGroup> GetMutualExclusionGroups(GameState state, ISimplificationTracker tracker) => [];

    public IEnumerable<DigitFence> GetFencedDigits(GameState state, ISimplificationTracker tracker)
    {
        List<DigitFence> fences = new(4);
        Cells cells = state.Cells;
        foreach (var quad in Quads)
        {
            var r = GetQuadRef(cells, quad.Coord);
            if (GetDigitFence(quad, quad.A, r) is {} a) fences.Add(a);
            if (GetDigitFence(quad, quad.B, r) is {} b) fences.Add(b);
            if (GetDigitFence(quad, quad.C, r) is {} c) fences.Add(c);
            if (GetDigitFence(quad, quad.D, r) is {} d) fences.Add(d);
        }
        return fences;

        DigitFence? GetDigitFence(Quad quad, CellValue target, ReadOnlyMultiRef<CellValueMask> r)
        {
            if (target == CellValue.None || r.Aggregate(false, (found, c) => found || c == target.AsMask()))
                return null;
            
            ReadOnlyMultiRef<CellValueMask> ar = cells.GetEmptyReferences();
            r.ForEach(
                (scoped ref readonly CellValueMask c, scoped ref ReadOnlyMultiRef<CellValueMask> a) =>
                {
                    if (c.Contains(target)) a.Include(in c);
                },
                ref ar
            );
            
            return new DigitFence(target, ar.Box(), tracker.Record($"quad at {quad.Coord} contains {target}"));
        }
    }

    public JsonObject ToJsonObject()
    {
        JsonArray quads = [];
        foreach (Quad quad in Quads)
        {
            JsonArray values = new(quad.A.NumericValue);
            if (quad.B != CellValue.None) values.Add(quad.B.NumericValue);
            if (quad.C != CellValue.None) values.Add(quad.C.NumericValue);
            if (quad.D != CellValue.None) values.Add(quad.D.NumericValue);
            quads.Add(new JsonObject { ["coord"] = new JsonArray(quad.Coord.Row, quad.Coord.Col), ["values"] = values });
        }

        return new JsonObject { ["quads"] = quads, };
    }

    public static IGameRule FromJsonObject(JsonObject jsonObject)
    {
        ImmutableArray<Quad>.Builder quads = ImmutableArray.CreateBuilder<Quad>();
        JsonArray quadObj = RuleHelpers.ValueOrThrow<JsonArray>(jsonObject, "quads");
        foreach (JsonNode quad in quadObj)
        {
            GridCoord coord = RuleHelpers.ValuesOrThrow<ushort, ushort>(RuleHelpers.ValueOrThrow<JsonArray>(quad, "coord"));
            var values = RuleHelpers.ValueOrThrow<JsonArray>(quad, "values");
            CellValue a = values.Count > 0 ? CellValue.FromNumericValue(values[0].GetValue<ushort>()) : CellValue.None;
            CellValue b = values.Count > 1 ? CellValue.FromNumericValue(values[1].GetValue<ushort>()) : CellValue.None;
            CellValue c = values.Count > 2 ? CellValue.FromNumericValue(values[2].GetValue<ushort>()) : CellValue.None;
            CellValue d = values.Count > 3 ? CellValue.FromNumericValue(values[3].GetValue<ushort>()) : CellValue.None;
            quads.Add(new Quad(coord, a, b, c, d));
        }
        return new QuadRule(quads.ToImmutable());
    }
}