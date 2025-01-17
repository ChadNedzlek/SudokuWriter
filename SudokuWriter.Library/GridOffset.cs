using System.Numerics;

namespace SudokuWriter.Library;

public readonly record struct GridOffset(short Row, short Col) :
    IAdditionOperators<GridOffset, GridCoord, GridCoord>,
    ISubtractionOperators<GridOffset, GridCoord, GridCoord>,
    IUnaryNegationOperators<GridOffset, GridOffset>
{
    public static implicit operator (short row, short col)(GridOffset c) => (c.Row, c.Col);

    public static implicit operator GridOffset((short row, short col) c) => new(c.row, c.col);

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

    public static GridOffset operator -(GridOffset value) => new((short)-value.Row, (short)-value.Col);
}