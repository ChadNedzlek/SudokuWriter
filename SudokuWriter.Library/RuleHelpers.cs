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