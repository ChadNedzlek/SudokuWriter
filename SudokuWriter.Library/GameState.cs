using System;
using System.Diagnostics;
using System.Text;

namespace VaettirNet.SudokuWriter.Library;

public readonly record struct GameStructure(ushort Rows, ushort Columns, ushort Digits, ushort BoxRows, ushort BoxColumns)
{
    public static GameStructure Default { get; } = new(9, 9, 9, 3, 3);
}

[DebuggerDisplay("{BoardString(),nq}")]
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

    public static GameState CreateFilled(GameStructure structure) => new(Cells.CreateFilled(structure), structure);

    public Cells Cells { get; }
    public GameStructure Structure { get; }
    public int BoxRows => Structure.BoxRows;
    public int BoxColumns => Structure.BoxColumns;
    public ushort Digits => Structure.Digits;
    public CellValue MinDigit { get; } = new(0);
    public CellValue MaxDigit => new((ushort)(Structure.Digits - 1));

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
        bool breakLines = Digits > 2 && Structure.Rows > 2;
        StringBuilder builder = new();
        for (int r = 0; r < Cells.Rows; r++)
        {
            builder.Append('[');
            for (int c = 0; c < Cells.Columns; c++)
                if (Cells[r, c] == CellValueMask.None)
                {
                    builder.Append("X ");
                }
                else
                {
                    for (ushort d = 0; d < Digits; d++)
                        if (Cells[r, c, new CellValue(d)])
                            builder.Append((char)('1' + d));

                    builder.Append(' ');
                }

            builder.Append(']');
            if (breakLines)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }
}