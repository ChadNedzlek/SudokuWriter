using System.Numerics;

namespace SudokuWriter.Library;

public readonly record struct GridCoord(ushort Row, ushort Col) :
    IAdditionOperators<GridCoord, GridCoord, GridCoord>,
    ISubtractionOperators<GridCoord, GridCoord, GridCoord>
{
    public static GridCoord operator +(GridCoord left, GridCoord right)
    {
        return unchecked(new((ushort)(left.Row + right.Row), (ushort)(left.Col + right.Col)));
    }
    
    public static GridCoord operator -(GridCoord left, GridCoord right)
    {
        return unchecked(new((ushort)(left.Row - right.Row), (ushort)(left.Col - right.Col)));
    }
    
    public static implicit operator GridCoord((ushort row, ushort col) c) => new (c.row, c.col);
    public static implicit operator GridCoord((int row, int col) c) => new ((ushort)c.row, (ushort)c.col);
}