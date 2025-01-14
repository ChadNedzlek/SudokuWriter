using System;
using System.Diagnostics;
using System.Text;

namespace SudokuWriter.Library;

public readonly record struct GameStructure(int Rows, int Columns, int Digits, int BoxRows, int BoxColumns)
{
    public static GameStructure Default { get; } = new(9, 9, 9, 3, 3);
}

[DebuggerDisplay("Board: {BoardString(),nq}")]
public readonly struct GameState
{
    public static GameState Default { get; } = new(Cells.CreateFilled(), GameStructure.Default);

    public GameState(Cells cells, GameStructure structure)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(cells.Rows, structure.Rows);
        ArgumentOutOfRangeException.ThrowIfNotEqual(cells.Columns, structure.Columns);
        Cells = cells;
        Structure = structure;
    }

    public Cells Cells { get; }
    public GameStructure Structure { get; }
    public int BoxRows => Structure.BoxRows;
    public int BoxColumns => Structure.BoxColumns;
    public int Digits => Structure.Digits;

    public GameState WithCells(Cells cells)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(cells.Rows, Structure.Rows);
        ArgumentOutOfRangeException.ThrowIfNotEqual(cells.Columns, Structure.Columns);
        return new GameState(cells, Structure);
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