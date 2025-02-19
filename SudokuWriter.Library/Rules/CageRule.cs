using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text.Json.Nodes;

namespace VaettirNet.SudokuWriter.Library.Rules;

using Vec = Vector256<ushort>;

[GameRule("cage")]
public class CageRule : IGameRule
{
    public CageRule(ushort sum, ImmutableArray<GridCoord> cage)
    {
        Sum = sum;
        Cage = cage;
    }

    public ushort Sum { get; }
    public ImmutableArray<GridCoord> Cage { get; }
    
    public GameResult Evaluate(GameState state)
    {
        ushort simpleSum = 0;
        bool allSet = true;
        List<CellValueMask> highCellValues = new (Cage.Length);
        foreach (GridCoord cell in Cage)
        {
            CellValueMask value = state.Cells[cell];
            highCellValues.Add(value);
            if (value.TryGetSingle(out var v))
            {
                simpleSum += v.NumericValue;
            }
            else
            {
                allSet = false;
            }
        }

        if (allSet)
        {
            return simpleSum == Sum ? GameResult.Solved : GameResult.Unsolvable;
        }

        List<CellValueMask> lowCellValues = highCellValues.ToList();


        highCellValues.Sort(HighestHighBitComparison);
        lowCellValues.Sort(LowestLowBitComparison);
        Span<CellValueMask> highSpan = CollectionsMarshal.AsSpan(highCellValues);
        Span<CellValueMask> lowSpan = CollectionsMarshal.AsSpan(lowCellValues);
        
        ushort minValue = 0;
        ushort maxValue = 0;

        for (int i = 0; i < Cage.Length; i++)
        {
            CellValueMask max = highSpan[i].GetMaxDigitMask();
            CellValueMask min = lowSpan[i].GetMinDigitMask();
            minValue += min.GetSingle().NumericValue;
            maxValue +=  max.GetSingle().NumericValue;
            for (int j = i + 1; j < Cage.Length; j++)
            {
                highSpan[j] &= ~max;
                lowSpan[j] &= ~min;
            }
            highCellValues.Sort(HighestHighBitComparison);
            lowCellValues.Sort(LowestLowBitComparison);
        }

        return minValue > Sum || maxValue < Sum
            ? GameResult.Unsolvable
            : minValue == maxValue
                ? GameResult.Solved
                : GameResult.Unknown;
        
        int HighestHighBitComparison(CellValueMask a, CellValueMask b) => b.RawValue.CompareTo(a.RawValue);
        int LowestLowBitComparison(CellValueMask a, CellValueMask b) => ushort.TrailingZeroCount(a.RawValue).CompareTo(ushort.TrailingZeroCount(b.RawValue));
    }

    public GameState? TryReduce(GameState state, ISimplificationChain chain)
    {
        if (state.Digits > 16) return null;
        CellsBuilder cells = state.Cells.ToBuilder();
        Vec setExcept = Vec.Zero;
        Vec indices = Vec.Indices;
        for (int i = 0; i < Cage.Length; i++)
        {
            Vec value = Vector256.Create(cells[Cage[i]].RawValue);
            Vec index = Vector256.Create((ushort)i);
            setExcept = Vector256.ConditionalSelect(Vector256.Equals(index, indices), setExcept, setExcept | value);
        }

        Vec minSumExcept = Vec.Zero;
        Vec maxSumExcept = Vec.Zero;
        Vec needMin = Vector256.Create((ushort)(Cage.Length - 1));
        Vec needMax = needMin;
        bool reduced = false;
        Vec minDigit = Vec.One;
        Vec maxDigit = Vector256.Create(state.Digits);
        Vec minMask = Vec.One;
        Vec maxMask = Vector256.ShiftLeft(Vec.One, state.Digits - 1);
        for (int i = 0; i < state.Digits; i++)
        {
            Vec fDigitInOtherCellsMin = Vector256.GreaterThan(setExcept & minMask, Vec.Zero);
            Vec fEmptyCellsMin = Vector256.GreaterThan(needMin, Vec.Zero);
            Vec fAddDigitMin = fDigitInOtherCellsMin & fEmptyCellsMin;
            minSumExcept = Vector256.ConditionalSelect(fAddDigitMin, minSumExcept + minDigit, minSumExcept);
            needMin = Vector256.ConditionalSelect(fAddDigitMin, needMin - Vec.One, needMin);
            minMask <<= 1;
            minDigit += Vec.One;

            Vec fDigitInOtherCellsMax = Vector256.GreaterThan(setExcept & maxMask, Vec.Zero);
            Vec fEmptyCellsMax = Vector256.GreaterThan(needMax, Vec.Zero);
            Vec fAddDigitMax = fDigitInOtherCellsMax & fEmptyCellsMax;
            maxSumExcept = Vector256.ConditionalSelect(fAddDigitMax, maxSumExcept + maxDigit, maxSumExcept);
            needMax = Vector256.ConditionalSelect(fAddDigitMax, needMax - Vec.One, needMax);
            maxMask >>= 1;
            maxDigit -= Vec.One;
        }

        Vec validMask = CalculateValidMask(maxSumExcept, minSumExcept, state.Digits);

        for (int i = 0; i < Cage.Length; i++)
        {
            reduced |= RuleHelpers.TryMask(ref cells[Cage[i]], new CellValueMask(validMask[i]));
        }

        return reduced ? state.WithCells(cells.MoveToImmutable()) : null;
    }

    public IEnumerable<MutexGroup> GetMutualExclusionGroups(GameState state, ISimplificationTracker tracker)
    {
        var refs = state.Cells.GetEmptyReferences();
        foreach (var cell in Cage)
        {
            refs.Include(in state.Cells[cell]);
        }

        return [new MutexGroup(refs.Box(), tracker.Record($"Cage {Sum} in cell {Cage[0]}"))];
    }

    public IEnumerable<DigitFence> GetFencedDigits(GameState state, ISimplificationTracker tracker)
    {
        // TODO: This should be possible if any given set might have full
        return [];
    }

    private Vec CalculateValidMask(Vec maxSumExcept, Vec minSumExcept, int digits)
    {
        if (Avx10v1.IsSupported)
        {
            return CalculateValidMaskAvx10v1(maxSumExcept, minSumExcept, digits);
        }

        return CalculateValidMaskAvx2(maxSumExcept, minSumExcept, digits);
    }

    private Vec CalculateValidMaskAvx2(Vec maxSumExcept, Vec minSumExcept, int digits)
    {
        var allMask = Vector256.Create((uint)CellValueMask.All(digits).RawValue);
        // The minimum value for any given cell is the Sum minus the max values of all the other cells
        // Ex.  If the sum is "20", and the other cells can only add to "15", then a cell has to be
        // a minimum of 5.  (so 20 - 1 - 15 = 4 = the amount we have to shift the mask)
        Vec minValue = Avx2.SubtractSaturate(Vector256.Create(Sum), maxSumExcept);
        Vec minValueMaskShift = Avx2.SubtractSaturate(minValue, Vec.One);
        
        Vec maxValue = Avx2.SubtractSaturate(Vector256.Create(Sum), minSumExcept);
        Vec maxValueMaskShift = Avx2.SubtractSaturate(Vector256.Create((ushort)digits), maxValue);
        (Vector256<uint> minLow, Vector256<uint> minHigh) = Vector256.Widen(minValueMaskShift);
        Vector256<uint> minMaskLow = Avx2.ShiftLeftLogicalVariable(allMask, minLow);
        Vector256<uint> minMaskHigh = Avx2.ShiftLeftLogicalVariable(allMask, minHigh);
        (Vector256<uint> maxLow, Vector256<uint> maxHigh) = Vector256.Widen(maxValueMaskShift);
        Vector256<uint> maxMaskLow = Avx2.ShiftRightLogicalVariable(allMask, maxLow);
        Vector256<uint> maxMaskHigh = Avx2.ShiftRightLogicalVariable(allMask, maxHigh);
        Vec minValueMask = Vector256.Narrow(minMaskLow, minMaskHigh);
        Vec maxValueMask = Vector256.Narrow(maxMaskLow, maxMaskHigh);
        Vec validMask = minValueMask & maxValueMask;
        return validMask;
    }

    // ReSharper disable once InconsistentNaming
    private Vec CalculateValidMaskAvx10v1(Vec maxSumExcept, Vec minSumExcept, int digits)
    {
        var allMask = Vector256.Create(CellValueMask.All(digits).RawValue);
        // The minimum value for any given cell is the Sum minus the max values of all the other cells
        // Ex.  If the sum is "20", and the other cells can only add to "15", then a cell has to be
        // a minimum of 5.  (so 20 - 1 - 15 = 4 = the amount we have to shift the mask)
        Vec minValue = Avx2.SubtractSaturate(Vector256.Create(Sum), maxSumExcept);
        Vec minValueMaskShift = Avx2.SubtractSaturate(minValue, Vec.One);
        
        Vec maxValue = Avx2.SubtractSaturate(Vector256.Create(Sum), minSumExcept);
        Vec maxValueMaskShift = Avx2.SubtractSaturate(Vector256.Create((ushort)digits), maxValue);
        Vec minValueMask = Avx10v1.ShiftLeftLogicalVariable(allMask, minValueMaskShift);
        Vec maxValueMask = Avx10v1.ShiftRightLogicalVariable(allMask, maxValueMaskShift);
        Vec validMask = minValueMask & maxValueMask;
        return validMask;
    }

    public JsonObject ToJsonObject()
    {
        return new JsonObject
        {
            ["sum"] = Sum,
            ["cage"] = RuleHelpers.WriteGridCoords(Cage),
        };
    }

    public static IGameRule FromJsonObject(JsonObject jsonObject)
    {
        ushort sum = RuleHelpers.ValueOrThrow<ushort>(jsonObject, "sum");
        ImmutableArray<GridCoord> cage = RuleHelpers.ReadGridCoords(jsonObject, "cage");
        return new CageRule(sum, cage);
    }
}