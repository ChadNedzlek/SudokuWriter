using System;
using System.Numerics;

namespace VaettirNet.SudokuWriter.Library;

public readonly struct CellValue :
    IEqualityOperators<CellValue, CellValue, bool>,
    IShiftOperators<CellValue, ushort, CellValue>,
    IComparisonOperators<CellValue, CellValue, bool>,
    IAdditionOperators<CellValue, ushort, CellValue>,
    ISubtractionOperators<CellValue, ushort, CellValue>,
    IAdditionOperators<CellValue, CellValue, CellValue>,
    ISubtractionOperators<CellValue, CellValue, CellValue>,
    IEquatable<CellValue>
{
    public static readonly CellValue None = new(ushort.MaxValue);
    
    private readonly ushort _value;

    public CellValue(ushort value)
    {
        _value = value;
    }

    public ushort NumericValue => (ushort)(_value + 1);

    public CellValueMask AsMask() => new ((ushort)(1 << _value));

    public ushort Serialize() => _value;
    public static CellValue Deserialize(ushort value) => new (value);

    public bool Equals(CellValue other) => _value == other._value;
    public override bool Equals(object obj) => obj is CellValue other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
    public static bool operator ==(CellValue left, CellValue right) => left._value == right._value;
    public static bool operator !=(CellValue left, CellValue right) => left._value != right._value;

    public static CellValueMask operator |(CellValue left, CellValue right) => left.AsMask() | right.AsMask();
    public static CellValue operator <<(CellValue value, ushort shiftAmount) => new((ushort)(value._value + shiftAmount));
    public static CellValue operator >> (CellValue value, ushort shiftAmount) => new((ushort)(value._value - shiftAmount));
    public static CellValue operator >>> (CellValue value, ushort shiftAmount) => new((ushort)(value._value - shiftAmount));
    public static bool operator >(CellValue left, CellValue right) => left._value > right._value;
    public static bool operator >=(CellValue left, CellValue right) => left._value >= right._value;
    public static bool operator <(CellValue left, CellValue right) => left._value < right._value;
    public static bool operator <=(CellValue left, CellValue right) => left._value <= right._value;
    public static CellValue operator +(CellValue left, ushort right) => new((ushort)(left._value + right));
    public static CellValue operator -(CellValue left, ushort right) => new((ushort)(left._value - right));
    public static CellValue operator +(CellValue left, CellValue right) => new((ushort)(left._value + right._value));
    public static CellValue operator -(CellValue left, CellValue right) => new((ushort)(left._value - right._value));
}