using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
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

    private readonly CancellationTokenSource _exit = new();

    private GameEngine _gameEngine = GameEngine.Default;
    private readonly AutoResetEventAsync _gameStateAvailable = new();

    private readonly ConcurrentQueue<GameQuery> _queries = new();

    private int _selfTriggered;
    private Task _solveTask;
    private readonly GameEngineSerializer _serializer;

    public MainWindow()
    {
        InitializeComponent();

        List<TextBox> cells = GetDescendants<TextBox>(GameGrid);
        _cellLayout = new Lazy<ImmutableList<TextBox>>(() => PopulateCells(cells));
        _solveTask = Task.Run(SolveQueries);
        _serializer = new GameEngineSerializer();
    }

    private ImmutableList<TextBox> CellBoxes => _cellLayout.Value;

    protected override void OnClosing(CancelEventArgs e)
    {
        _exit.Cancel();
        base.OnClosing(e);
    }

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
            if (_selfTriggered != 0) return;

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
        if (_gameEngine.Rules.IsEmpty) return;
        
        GameState state = BuildGameStateFromUi();

        TaskCompletionSource<GameQueryResult> resultTask = new();
        _queries.Enqueue(new GameQuery(state, resultTask));
        _gameStateAvailable.Trigger();
        GameQueryResult result = await resultTask.Task;

        Style s = result.Result switch
        {
            GameResult.Unsolvable => GameGrid.TryFindResource("UnsolvableGame") as Style,
            GameResult.Solved => GameGrid.TryFindResource("SolvedGame") as Style,
            GameResult.MultipleSolutions => GameGrid.TryFindResource("AmbiguousGame") as Style,
            _ => null
        };
        foreach (Border border in GetDescendants<Border>(GameGrid)) border.Style = s;

        ImmutableList<TextBox> uiCells = CellBoxes;
        
        for (int r = 0; r < state.Structure.Rows; r++)
        for (int c = 0; c < state.Structure.Columns; c++)
        {
            TextBox uiCell = uiCells[r * 9 + c];
            CellStyle style = uiCell.Tag is CellValue value ? value.Style : CellStyle.Ambiguous;
            if (style == CellStyle.Fixed) continue;
            switch (result.Result)
            {
                case GameResult.Unsolvable:
                    SetCell(r, c, "", CellStyle.Potential);
                    break;
                case GameResult.Solved:
                    SetCell(
                        r,
                        c,
                        TextFromDigit(result.PrimaryState.Value.Cells.GetSingle(r, c)),
                        CellStyle.Solved
                    );
                    break;
                case GameResult.MultipleSolutions:
                    ushort mask = (ushort)(result.PrimaryState.Value.Cells.GetMask(r, c) |
                        result.ConflictingState.Value.Cells.GetMask(r, c));
                    if (BitOperations.IsPow2(mask))
                        SetCell(
                            r,
                            c,
                            TextFromDigitMask(
                                mask
                            ),
                            CellStyle.Solved
                        );
                    else
                        SetCell(
                            r,
                            c,
                            TextFromDigitMask(
                                mask
                            ),
                            CellStyle.Ambiguous
                        );

                    break;
            }
        }
    }

    private GameState BuildGameStateFromUi()
    {
        var state = new GameState(Cells.CreateFilled(_gameEngine.InitialState.Structure), _gameEngine.InitialState.Structure);
        CellsBuilder cells = state.Cells.ToBuilder();
        ImmutableList<TextBox> uiCells = CellBoxes;
        for (int r = 0; r < cells.Rows; r++)
        for (int c = 0; c < cells.Columns; c++)
        {
            if (uiCells[r * 9 + c].Tag is CellValue { Style: CellStyle.Fixed, Text: { } cellText })
            {
                cells.SetSingle(r, c, DigitFromText(cellText));
            }
        }

        state = state.WithCells(cells.MoveToImmutable());
        return state;
    }

    private ushort DigitFromText(string text)
    {
        return (ushort)(ushort.TryParse(text, out ushort digit) ? digit - 1 : ~0);
    }

    private string TextFromDigit(int digit)
    {
        return (digit + 1).ToString();
    }

    private string TextFromDigitMask(ushort digit)
    {
        var b = new StringBuilder(9);
        for (ushort d = 0; d < 9; d++)
            if (Cells.IsDigitSet(digit, d))
                b.Append(TextFromDigit(d));

        return b.ToString();
    }

    [DoesNotReturn]
    private async Task SolveQueries()
    {
        while (true)
        {
            _exit.Token.ThrowIfCancellationRequested();
            await _gameStateAvailable.WaitAsync();
            if (_queries.TryDequeue(out GameQuery s))
            {
                Debug.WriteLine("Found state to query", "INFO");
                GameResult result = _gameEngine.Evaluate(s.State, out GameState? solution, out GameState? conflict);
                Debug.WriteLine($"Query complete result={result}", "INFO");
                s.OnSolved.TrySetResult(new GameQueryResult(result, solution, conflict));
                Debug.WriteLine($"Dispatch query complete", "INFO");
            }
            else
            {
                Debug.WriteLine("No query found", "ERROR");
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

    private static List<T> GetDescendants<T>(FrameworkElement ctrl)
        where T : FrameworkElement
    {
        List<T> list = [];
        Queue<FrameworkElement> search = new();
        search.Enqueue(ctrl);
        while (search.TryDequeue(out FrameworkElement e))
            foreach (object child in LogicalTreeHelper.GetChildren(e))
                if (child is FrameworkElement fe)
                {
                    if (fe is T t) list.Add(t);

                    search.Enqueue(fe);
                }

        return list;
    }

    private void CellFocused(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TextBox box) return;

        if (box.Tag is CellValue { Style: CellStyle.Fixed }) return;

        SetCellTextAndStyle(box, "", CellStyle.Fixed);
    }

    private void CellUnfocused(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TextBox box) return;

        if (box.Tag is not CellValue computed) return;

        SetCellTextAndStyle(box, computed.Text, computed.Style);
    }

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
                if (i < 9 * 8) CellBoxes[i + 9].Focus();
                break;
            case Key.Left:
                if (i > 0) CellBoxes[i - 1].Focus();
                break;
            case Key.Right:
                if (i < 9 * 9) CellBoxes[i + 1].Focus();
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private record class GameQueryResult(GameResult Result, GameState? PrimaryState, GameState? ConflictingState);

    private record class GameQuery(GameState State, TaskCompletionSource<GameQueryResult> OnSolved);

    private readonly record struct CellValue(string Text, CellStyle Style);

    private void CloseWindow(object sender, ExecutedRoutedEventArgs e) => Close();

    private void NewGame(object sender, ExecutedRoutedEventArgs e)
    {
        _gameEngine = GameEngine.Default;
        _selfTriggered++;
        foreach (TextBox cell in CellBoxes)
        {
            cell.Text = "";
        }
        _selfTriggered--;
    }

    private async void SaveGame(object sender, ExecutedRoutedEventArgs e)
    {
        var dlg = new SaveFileDialog()
        {
            Filter = "Sudoku Game (*.sdku)|*.sdku|All Files (*.*)|*.*",
            DefaultExt = ".sdku",
            AddExtension = true,
            ValidateNames = true,
        };
        
        if (dlg.ShowDialog() is not true)
        {
            return;
        }
        
        GameState gameState = BuildGameStateFromUi();
        await using Stream stream = dlg.OpenFile();
        await _serializer.SaveGameAsync(_gameEngine.WithInitialState(gameState), stream);
    }

    private async void OpenGame(object sender, ExecutedRoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Sudoku Game (*.sdku)|*.sdku|All Files (*.*)|*.*",
            DefaultExt = ".sdku",
            Multiselect = false,
        };
        
        if (dlg.ShowDialog() is not true)
        {
            return;
        }
        
        _queries.Clear();
            
        await using Stream stream = dlg.OpenFile();
        GameEngine gameEngine = await _serializer.LoadGameAsync(stream);
        _gameEngine = gameEngine;
        for (int r = 0; r < gameEngine.InitialState.Structure.Rows; r++)
        for (int c = 0; c < gameEngine.InitialState.Structure.Columns; c++)
        {
            switch (gameEngine.InitialState.Cells.GetSingle(r, c))
            {
                case -1:
                    SetCell(r, c, "", CellStyle.Potential);
                    break;
                case var x:
                    SetCell(r, c, TextFromDigit(x), CellStyle.Fixed);
                    break;
            }
        }

        await ValidateAndReportGameState();
    }
}