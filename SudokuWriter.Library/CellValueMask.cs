using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace VaettirNet.SudokuWriter.Library;

[DebuggerDisplay("{ToString(),nq}")]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct CellValueMask :
    IBitwiseOperators<CellValueMask, CellValueMask, CellValueMask>,
    IBitwiseOperators<CellValueMask, CellValue, CellValueMask>,
    IShiftOperators<CellValueMask, ushort, CellValueMask>,
    IEqualityOperators<CellValueMask, CellValueMask, bool>,
    IEquatable<CellValueMask>
{

    public static readonly CellValueMask None = new(0);
    public static CellValueMask All(int digits) => new((ushort)((1 << digits) - 1));
    
    private readonly ushort _mask;

    internal CellValueMask(ushort mask)
    {
        _mask = mask;
    }

    public CellValue GetSingle() => BitOperations.IsPow2(_mask) ? new((ushort)BitOperations.Log2(_mask)) : CellValue.None;

    public bool TryGetSingle(out CellValue value)
    {
        if (BitOperations.IsPow2(_mask))
        {
            value = new((ushort)BitOperations.Log2(_mask));
            return true;
        }

        value = default;
        return false;
    }

    public ushort Count => ushort.PopCount(_mask);
    public ushort RawValue => _mask;

    public static CellValueMask operator &(CellValueMask left, CellValueMask right) => new((ushort)(left._mask & right._mask));

    public static CellValueMask operator |(CellValueMask left, CellValueMask right) => new((ushort)(left._mask | right._mask));

    public static CellValueMask operator ^(CellValueMask left, CellValueMask right) => new((ushort)(left._mask ^ right._mask));

    public static CellValueMask operator &(CellValueMask left, CellValue right) => left & right.AsMask();

    public static CellValueMask operator |(CellValueMask left, CellValue right) => left | right.AsMask();

    public static CellValueMask operator ^(CellValueMask left, CellValue right) => left ^ right.AsMask();

    public static CellValueMask operator &(CellValue left, CellValueMask right) => left.AsMask() & right;

    public static CellValueMask operator |(CellValue left, CellValueMask right) => left.AsMask() | right;

    public static CellValueMask operator ^(CellValue left, CellValueMask right) => left.AsMask() ^ right;

    public static CellValueMask operator ~(CellValueMask value) => new((ushort)~value._mask);

    public bool Contains(CellValue cellValue) => (_mask & cellValue.AsMask()._mask) != 0;
    
    public static bool operator ==(CellValueMask left, CellValueMask right) => left._mask == right._mask;

    public static bool operator !=(CellValueMask left, CellValueMask right) => left._mask != right._mask;

    public bool Equals(CellValueMask other) => _mask == other._mask;
    
    public override bool Equals(object obj)
    {
        return obj is CellValueMask other && Equals(other);
    }
    public override int GetHashCode()
    {
        return _mask.GetHashCode();
    }

    public bool IsSingle() => BitOperations.IsPow2(_mask);
    
    public CellValueMask GetMaxDigitMask() => new((ushort)(1 << (15 - ushort.LeadingZeroCount(_mask))));
    public CellValueMask GetMinDigitMask() => new((ushort)~(~_mask | (_mask - 1)));
    
    public static CellValueMask operator <<(CellValueMask value, ushort shiftAmount) => new((ushort)(value._mask << shiftAmount));
    public static CellValueMask operator <<(CellValueMask value, int shiftAmount) => new((ushort)(value._mask << shiftAmount));
    public static CellValueMask operator >> (CellValueMask value, ushort shiftAmount) => new((ushort)(value._mask >> shiftAmount));
    public static CellValueMask operator >> (CellValueMask value, int shiftAmount) => new((ushort)(value._mask >> shiftAmount));
    public static CellValueMask operator >>> (CellValueMask value, ushort shiftAmount) => new((ushort)(value._mask >>> shiftAmount));

    public CellValueMask Reversed(ushort digits)
    {
        ushort ret;
        unchecked
        {
            uint x = _mask;
            x |= (x & 0x000000FF) << 16;
            x = (x & 0xF0F0F0F0) | ((x & 0x0F0F0F0F) << 8);
            x = (x & 0xCCCCCCCC) | ((x & 0x33333333) << 4);
            x = (x & 0XAAAAAAAA) | ((x & 0x55555555) << 2);
            x <<= 1;
            x >>= 32 - digits;
            ret = (ushort)x;
        }

        return new CellValueMask(ret);
    }

    public CellValue GetMaxValue() => new((ushort)(16 - ushort.LeadingZeroCount(_mask)));
    public CellValue GetMinValue() => new(ushort.TrailingZeroCount(_mask));

    public override string ToString()
    {
        StringBuilder b = new();
        for (int i = 0; 1 << i <= _mask; i++)
        {
            if (((1 << i) & _mask) != 0)
            {
                b.Append((char)('1' + i));
            }
        }

        return b.ToString();
    }
}