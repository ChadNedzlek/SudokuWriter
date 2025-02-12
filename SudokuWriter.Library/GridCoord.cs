using System;
using System.Numerics;

namespace VaettirNet.SudokuWriter.Library;

public readonly record struct GridCoord(ushort Row, ushort Col) :
    IAdditionOperators<GridCoord, GridOffset, GridCoord>,
    ISubtractionOperators<GridCoord, GridOffset, GridCoord>,
    IEqualityOperators<GridCoord, GridCoord, bool>
{
    public static readonly GridCoord Invalid = new(ushort.MaxValue, ushort.MaxValue);
    
    public static GridCoord operator +(GridCoord left, GridOffset right)
    {
        return checked(new((ushort)(left.Row + right.Row), (ushort)(left.Col + right.Col)));
    }
    
    public static GridCoord operator -(GridCoord left, GridOffset right)
    {
        return new((ushort)(left.Row - right.Row), (ushort)(left.Col - right.Col));
    }
    
    public static GridCoord operator +(GridOffset left, GridCoord right)
    {
        return new((ushort)(right.Row + left.Row), (ushort)(right.Col + left.Col));
    }
    
    public static GridCoord operator -(GridOffset left, GridCoord right)
    {
        return new((ushort)(right.Row - left.Row), (ushort)(right.Col - left.Col));
    }

    public int DistanceTo(GridCoord other) => Math.Abs(other.Row - Row) + Math.Abs(other.Col - Col);
    
    public static implicit operator GridCoord((ushort row, ushort col) c) => new (c.row, c.col);
    public static implicit operator (int row, int col)(GridCoord c) => (c.Row, c.Col);

    public override string ToString() => $"({Row},{Col})";
}