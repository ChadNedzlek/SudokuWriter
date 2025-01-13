using System.Diagnostics;
using System.Text;

namespace SudokuWriter.Library;

[DebuggerDisplay("Board: {BoardString(),nq}")]
public readonly struct GameState
{
    public static GameState Default { get; } = new(new Cells(new ImmutableArray2<ushort>(9, 9)));

    public GameState(Cells cells, int boxRows = 3, int boxColumns = 3, int digits = 9)
    {
        Cells = cells;
        BoxRows = boxRows;
        BoxColumns = boxColumns;
        Digits = digits;
    }

    public Cells Cells { get; }
    public int BoxRows { get; }
    public int BoxColumns { get; }
    public int Digits { get; }

    public GameState WithCells(Cells cells)
    {
        return new GameState(cells, BoxRows, BoxColumns, Digits);
    }

    public ulong GetStateHash()
    {
        return Cells.GetCellHash();
    }

    public string BoardString()
    {
        StringBuilder builder = new();
        for (var r = 0; r < Cells.Rows; r++)
        {
            builder.Append('[');
            for (var c = 0; c < Cells.Columns; c++)
                if (Cells.GetMask(r, c) == 0)
                {
                    builder.Append("X ");
                }
                else
                {
                    for (var d = 0; d < Digits; d++)
                        if (Cells[r, c, d])
                            builder.Append((char)('1' + d));

                    builder.Append(' ');
                }

            builder.Append(']');
        }

        return builder.ToString();
    }
}