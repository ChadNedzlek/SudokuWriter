using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SudokuWriter.Library;

namespace SudokuWriter.Gui;

public partial class MainWindow : Window
{
    public enum CellStyle
    {
        Fixed,
        Solved,
        Potential,
        Ambiguous
    }

    private readonly Lazy<ImmutableList<TextBox>> _cellLayout;

    private GameEngine _gameEngine = GameEngine.Default;
    private Task _solveTask;

    private int _selfTriggered;

    public MainWindow()
    {
        InitializeComponent();

        List<TextBox> cells = GetDescendants<TextBox>(GameGrid);
        _cellLayout = new Lazy<ImmutableList<TextBox>>(() => PopulateCells(cells));
        _solveTask = Task.Run(SolveQueries);
    }

    private ImmutableList<TextBox> CellBoxes => _cellLayout.Value;

    private ImmutableList<TextBox> PopulateCells(List<TextBox> cells)
    {
        return cells.OrderBy(t => t.TransformToAncestor(GameGrid).Transform(default).Y)
            .ThenBy(t => t.TransformToAncestor(GameGrid).Transform(default).X)
            .ToImmutableList();
    }

    private async void CellChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (_selfTriggered != 0)
            {
                return;
            }

            if (e.OriginalSource is not TextBox cell) return;
            
            cell.Tag = new CellValue(cell.Text, CellStyle.Fixed);
            await ValidateAndReportGameState();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception: {ex}");
        }
    }

    private async Task ValidateAndReportGameState()
    {
        GameState state = GameState.Default;
        CellsBuilder cells = state.Cells.ToBuilder();
        var uiCells = CellBoxes;
        for (int r = 0; r < cells.Rows; r++)
        for (int c = 0; c < cells.Columns; c++)
        { 
            var uiCell = uiCells[r * 9 + c];
            if (uiCell.Tag is CellValue { Style: CellStyle.Fixed, Text: { } cellText } &&
                ushort.TryParse(cellText, out var value))
            {
                cells.SetSingle(r, c, (ushort)(value - 1));
            }
        }

        TaskCompletionSource<GameQueryResult> resultTask = new();
        _queries.Enqueue(new GameQuery(state.WithCells(cells.MoveToImmutable()), resultTask));
        Interlocked.Exchange(ref _gameStateAvailable, new TaskCompletionSource()).SetResult();
        GameQueryResult result = await resultTask.Task;

        Style s = result.Result switch
        {
            GameResult.Unsolvable => GameGrid.TryFindResource("UnsolvableGame") as Style,
            GameResult.Solved => GameGrid.TryFindResource("SolvedGame") as Style,
            GameResult.MultipleSolutions => GameGrid.TryFindResource("AmbiguousGame") as Style,
            _ => null,
        };
        foreach (var border in GetDescendants<Border>(GameGrid))
        {
            border.Style = s;
        }

        for (int r = 0; r < cells.Rows; r++)
        for (int c = 0; c < cells.Columns; c++)
        {
            TextBox uiCell = uiCells[r * 9 + c];
            var style = uiCell.Tag is CellValue value ? value.Style : CellStyle.Ambiguous;
            if (style == CellStyle.Fixed) continue;
            switch (result.Result)
            {
                case GameResult.Unsolvable:
                    SetCell(r, c, "", CellStyle.Potential);
                    break;
                case GameResult.Solved:
                    SetCell(r,
                        c,
                        TextFromDigit(result.PrimaryState.Value.Cells.GetSingle(r, c)),
                        CellStyle.Solved);
                    break;
                case GameResult.MultipleSolutions:
                    var mask = (ushort)(result.PrimaryState.Value.Cells.GetMask(r, c) |
                        result.ConflictingState.Value.Cells.GetMask(r, c));
                    if (BitOperations.IsPow2(mask))
                    {
                        SetCell(r,
                            c,
                            TextFromDigitMask(
                                mask),
                            CellStyle.Solved);
                    }
                    else
                    {
                        SetCell(r,
                            c,
                            TextFromDigitMask(
                                mask),
                            CellStyle.Ambiguous);
                    }

                    break;
            }
        }
    }

    private ushort DigitFromText(string text) => (ushort)(ushort.TryParse(text, out var digit) ? digit - 1 : ~0);
    private string TextFromDigit(int digit) => (digit + 1).ToString();

    private string TextFromDigitMask(ushort digit)
    {
        StringBuilder b = new StringBuilder(9);
        for (ushort d = 0; d < 9; d++)
        {
            if (Cells.IsDigitSet(digit, d))
            {
                b.Append(TextFromDigit(d));
            }
        }

        return b.ToString();
    }

    private record class GameQueryResult(GameResult Result, GameState? PrimaryState, GameState? ConflictingState);
    
    private record class GameQuery(GameState State, TaskCompletionSource<GameQueryResult> OnSolved);

    private ConcurrentQueue<GameQuery> _queries = new();

    private CancellationTokenSource _exit = new ();
    private TaskCompletionSource _gameStateAvailable = new TaskCompletionSource();
    
    [DoesNotReturn]
    private async Task SolveQueries()
    {
        while (true)
        {
            _exit.Token.ThrowIfCancellationRequested();
            await _gameStateAvailable.Task;
            if (_queries.TryDequeue(out var s))
            {
                var result = _gameEngine.Evaluate(s.State, out var solution, out var conflict);
                s.OnSolved.TrySetResult(new(result, solution, conflict));
            }
            else
            {
            }
        }
    }

    public void SetCell(int row, int column, string text, CellStyle style)
    {
        TextBox cell = CellBoxes[row * 9 + column];
        SetCellTextAndStyle(cell, text, style);
        cell.Tag = new CellValue(text, style);
    }

    private void SetCellTextAndStyle(TextBox cell, string text, CellStyle style)
    {
        _selfTriggered++;
        try
        {
            cell.Text = text;
            cell.Style = GameGrid.TryFindResource(style.ToString()) as Style;
        }
        finally
        {
            _selfTriggered--;
        }
    }

    private static List<T> GetDescendants<T>(FrameworkElement ctrl) where T : FrameworkElement
    {
        List<T> list = [];
        Queue<FrameworkElement> search = new();
        search.Enqueue(ctrl);
        while (search.TryDequeue(out FrameworkElement e))
            foreach (object child in LogicalTreeHelper.GetChildren(e))
                if (child is FrameworkElement fe)
                {
                    if (fe is T t)
                    {
                        list.Add(t);
                    }

                    search.Enqueue(fe);
                }

        return list;
    }

    private void CellFocused(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TextBox box)
        {
            return;
        }
        
        if (box.Tag is CellValue { Style: CellStyle.Fixed })
        {
            return;
        }

        SetCellTextAndStyle(box, "", CellStyle.Fixed);
    }

    private void CellUnfocused(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TextBox box)
        {
            return;
        }

        if (box.Tag is not CellValue computed)
        {
            return;
        }

        SetCellTextAndStyle(box, computed.Text, computed.Style);
    }

    private readonly record struct CellValue(string Text, CellStyle Style);

    private void GridPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.OriginalSource is not TextBox box) return;

        int i = CellBoxes.IndexOf(box);
        if (i == -1) return;
        
        switch (e.Key)
        {
            case Key.Up:
                if (i > 9) CellBoxes[i - 9].Focus();
                break;
            case Key.Down:
                if (i < (9*8)) CellBoxes[i + 9].Focus();
                break;
            case Key.Left:
                if (i > 0) CellBoxes[i - 1].Focus();
                break;
            case Key.Right:
                if (i < 9*9) CellBoxes[i + 1].Focus();
                break;
            default:
                return;
        }

        e.Handled = true;
    }
}