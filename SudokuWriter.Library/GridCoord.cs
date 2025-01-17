using System.Numerics;

namespace SudokuWriter.Library;

public readonly record struct GridCoord(ushort Row, ushort Col) :
    IAdditionOperators<GridCoord, GridOffset, GridCoord>,
    ISubtractionOperators<GridCoord, GridOffset, GridCoord>,
    IEqualityOperators<GridCoord, GridCoord, bool>
{
    public static GridCoord operator +(GridCoord left, GridOffset right)
    {
        return checked(new((ushort)(left.Row + right.Row), (ushort)(left.Col + right.Col)));
    }
    
    public static GridCoord operator -(GridCoord left, GridOffset right)
    {
        return checked(new((ushort)(left.Row - right.Row), (ushort)(left.Col - right.Col)));
    }
    
    public static GridCoord operator +(GridOffset left, GridCoord right)
    {
        return checked(new((ushort)(right.Row + left.Row), (ushort)(right.Col + left.Col)));
    }
    
    public static GridCoord operator -(GridOffset left, GridCoord right)
    {
        return checked(new((ushort)(right.Row - left.Row), (ushort)(right.Col - left.Col)));
    }
    
    public static implicit operator GridCoord((ushort row, ushort col) c) => new (c.row, c.col);
    public static implicit operator (int row, int col)(GridCoord c) => (c.Row, c.Col);
}