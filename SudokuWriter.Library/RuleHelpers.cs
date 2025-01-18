using System.Collections.Immutable;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Threading;
using SudokuWriter.Library.CellAdjacencies;
using SudokuWriter.Library.Rules;

namespace SudokuWriter.Library;

public static class RuleHelpers
{
    public static ImmutableList<GridCoord> ReadGridCoords(JsonNode node, string key)
    {
        JsonNode prop = node[key];
        if (prop is null)
            throw new InvalidDataException($"Expected key {key} is not present");
        if (prop is not JsonArray arr)
            throw new InvalidDataException($"Expected key {key} is not an array");
        return ReadGridCoords(arr);
    }

    public static ImmutableList<GridCoord> ReadGridCoords(JsonArray array)
    {
        ImmutableList<GridCoord>.Builder b = ImmutableList.CreateBuilder<GridCoord>();
        for (int i = 0; i < array.Count; i++)
        {
            switch (array[i])
            {
                case JsonArray a:
                    b.Add(ValuesOrThrow<ushort, ushort>(a));
                    break;
                case var row:
                    ushort col = array[i++].GetValue<ushort>();
                    b.Add(new GridCoord(row.GetValue<ushort>(), col));
                    break;
            }
        }

        return b.ToImmutable();
    }

    public static JsonArray WriteGridCoords(ImmutableList<GridCoord> coords)
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

    public static bool ClearFromSeenCells(ushort inputCell, scoped in MultiRef<ushort> seenCells)
    {
        int single = Cells.GetSingle(inputCell);
        if (single == -1) return false;

        ushort mask = unchecked((ushort)~inputCell);

        return seenCells.Aggregate(
            (bool c, ref ushort cell) => c | TryMask(ref cell, mask)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryUpdate<T>(ref T value, T newValue)
        where T : unmanaged, IEqualityOperators<T, T, bool>
        => Interlocked.Exchange(ref value, newValue) != newValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryMask<T>(ref T value, T mask)
        where T : unmanaged, IEqualityOperators<T, T, bool>, IBitwiseOperators<T, T, T>
        => TryUpdate(ref value, value & mask);
}