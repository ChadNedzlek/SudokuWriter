using System;
using System.Diagnostics;
using System.Numerics;

namespace VaettirNet.SudokuWriter.Library;

[DebuggerDisplay("{NumericValue,nq}")]
public readonly struct CellValue :
    IComparisonOperators<CellValue, CellValue, bool>,
    IAdditionOperators<CellValue, CellValue, CellValue>,
    IAdditionOperators<CellValue, ushort, CellValue>,
    ISubtractionOperators<CellValue, CellValue, CellValue>,
    ISubtractionOperators<CellValue, ushort, CellValue>,
    IUnaryPlusOperators<CellValue, CellValue>,
    IIncrementOperators<CellValue>,
    IDecrementOperators<CellValue>,
    IEquatable<CellValue>
{
    public static readonly CellValue None = new(ushort.MaxValue);

    private readonly ushort _value;

    public CellValue(ushort value)
    {
        _value = value;
    }

    public ushort NumericValue => (ushort)(_value + 1);
    public static CellValue FromNumericValue(ushort value) => value == 0 ? None : new((ushort)(value - 1));

    public CellValueMask AsMask() => new((ushort)(1 << _value));

    public ushort Serialize() => _value;
    public static CellValue Deserialize(ushort value) => new(value);

    public bool Equals(CellValue other) => _value == other._value;
    public static CellValue operator +(CellValue value) => value;
    public override bool Equals(object obj) => obj is CellValue other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
    public static bool operator ==(CellValue left, CellValue right) => left._value == right._value;
    public static bool operator !=(CellValue left, CellValue right) => left._value != right._value;

    public static CellValueMask operator |(CellValue left, CellValue right) =>
        left == None ? right.AsMask() : right == None ? left.AsMask() : left.AsMask() | right.AsMask();
    
    public static bool operator >(CellValue left, CellValue right) => left._value > right._value;
    public static bool operator >=(CellValue left, CellValue right) => left._value >= right._value;
    public static bool operator <(CellValue left, CellValue right) => left._value < right._value;
    public static bool operator <=(CellValue left, CellValue right) => left._value <= right._value;
    public static CellValue operator +(CellValue left, ushort right) => new((ushort)(left._value + right));
    public static CellValue operator -(CellValue left, ushort right) => new((ushort)(left._value - right));
    public static CellValue operator +(CellValue left, CellValue right) => new((ushort)(left._value + right._value));
    public static CellValue operator -(CellValue left, CellValue right) => new((ushort)(left._value - right._value));
    public static CellValue operator ++(CellValue value) => value + 1;
    public static CellValue operator --(CellValue value) => value - 1;

    public static CellValue Min(CellValue left, CellValue right) => left._value < right._value ? left : right;
    public static CellValue Max(CellValue left, CellValue right) => left._value > right._value ? left : right;

    public override string ToString() => NumericValue.ToString();
}