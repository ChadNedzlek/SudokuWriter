using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading;

namespace VaettirNet.SudokuWriter.Library;

public static class RuleHelpers
{
    public static ImmutableArray<GridCoord> ReadGridCoords(JsonNode node, string key)
    {
        JsonNode prop = node[key];
        if (prop is null)
            throw new InvalidDataException($"Expected key {key} is not present");
        if (prop is not JsonArray arr)
            throw new InvalidDataException($"Expected key {key} is not an array");
        return ReadGridCoords(arr);
    }

    public static ImmutableArray<GridCoord> ReadGridCoords(JsonArray array)
    {
        ImmutableArray<GridCoord>.Builder b = ImmutableArray.CreateBuilder<GridCoord>();
        for (int i = 0; i < array.Count; i++)
        {
            switch (array[i])
            {
                case JsonArray a:
                    b.Add(ValuesOrThrow<ushort, ushort>(a));
                    break;
                case var row:
                    ushort col = array[++i].GetValue<ushort>();
                    b.Add(new GridCoord(row.GetValue<ushort>(), col));
                    break;
            }
        }

        return b.ToImmutable();
    }

    public static JsonArray WriteGridCoords(ImmutableArray<GridCoord> coords)
    {
        JsonArray arr = new JsonArray();
        foreach (GridCoord c in coords)
        {
            arr.Add(c.Row);
            arr.Add(c.Col);
        }

        return arr;
    }

    public static T ValueOrThrow<T>(JsonNode node, string key)
    {
        JsonNode prop = node[key];
        if (prop is null)
            throw new InvalidDataException($"Expected key {key} is not present");
        return prop.GetValue<T>();
    }

    public static (T1 a, T2 b) ValuesOrThrow<T1, T2>(JsonArray node)
    {
        if (node.Count != 2)
            throw new InvalidDataException($"Array should be 2 long, but was {node.Count}");
        return (node[0].GetValue<T1>(), node[1].GetValue<T2>());
    }

    public static bool ClearFromSeenCells(CellValueMask inputCell, scoped in MultiRef<CellValueMask> seenCells)
    {
        if (!inputCell.IsSingle()) return false;

        return seenCells.Aggregate(false, TryMask, ~inputCell);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryUpdate(ref CellValueMask value, CellValueMask newValue)
        => Interlocked.Exchange(ref Unsafe.As<CellValueMask, ushort>(ref value), newValue.RawValue) != newValue.RawValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryMask(ref CellValueMask value, CellValueMask mask) => TryUpdate(ref value, value & mask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryMask(bool previous, ref CellValueMask value, CellValueMask mask) => TryUpdate(ref value, value & mask) | previous;
}

[InterpolatedStringHandler]
public struct SimplificationInterpolationHandler
{
    private List<object> _parts;

    private void AddPart(object o)
    {
        if (!Tracker.IsTracking) return;
        
        (_parts ??= new List<object>()).Add(o);
    }

    
    public SimplificationInterpolationHandler(int literalLength, int formattedCount, ISimplificationTracker tracker)
    {
        Tracker = tracker;
        _parts = tracker.IsTracking ? new List<object>(formattedCount * 2 + 1) : null;
    }
    
    public SimplificationInterpolationHandler(int literalLength, int formattedCount, ISimplificationChain tracker) : this(literalLength, formattedCount, tracker.Tracker)
    {
    }

    public ISimplificationTracker Tracker { get; set; }

    public void AppendLiteral(string s)
    {
        AddPart(s);
    }

    public void AppendFormatted<T>(T value)
    {
        AddPart(value);
    }
    
    public void AppendFormatted(ReadOnlyMultiRef<CellValueMask> value)
    {
        if (!Tracker.IsTracking) return;
        
        (_parts ??= new List<object>()).Add(value.Render());
    }

    public SimplificationRecord Build()
    {
        if (!Tracker.IsTracking)
        {
            return SimplificationRecord.Empty;
        }

        return new SimplificationRecord(string.Join("", _parts));
    }
}

public sealed class NoopTracker : ISimplificationTracker
{
    private readonly ISimplificationChain _emptyChain = new NoopChain();
    public bool IsTracking => false;
    public static NoopTracker Instance { get; } = new();
    public ISimplificationChain GetEmptyChain() => _emptyChain;

    public SimplificationRecord Record(SimplificationInterpolationHandler record) => SimplificationRecord.Empty;

    private class NoopChain : ISimplificationChain
    {
        public void Record(SimplificationRecord record)
        {
        }

        public ISimplificationTracker Tracker => Instance;
    }

}

public interface ISimplificationTracker
{
    bool IsTracking { get; }

    ISimplificationChain GetEmptyChain();
    SimplificationRecord Record([InterpolatedStringHandlerArgument("")] SimplificationInterpolationHandler record);
}

public interface ISimplificationChain
{
    void Record(SimplificationRecord record);
    ISimplificationTracker Tracker { get; }
}

public static class SimplificationChainExtensions
{
    public static void Record(
        this ISimplificationChain chain,
        [InterpolatedStringHandlerArgument(nameof(chain))] SimplificationInterpolationHandler record
    ) =>
        chain.Record(record.Tracker.Record(record));
}

public readonly record struct SimplificationRecord(string Description)
{
    public static  SimplificationRecord Empty = new();
}