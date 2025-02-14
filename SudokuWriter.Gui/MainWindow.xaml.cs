using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using VaettirNet.SudokuWriter.Gui.UiRules;
using VaettirNet.SudokuWriter.Library;
using VaettirNet.SudokuWriter.Library.Rules;
using VaettirNet.VelopackExtensions.SignedReleases;
using VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;
using Velopack;

namespace VaettirNet.SudokuWriter.Gui;

public partial class MainWindow
{
    private readonly IVelopackAssetValidator _assetValidator;
    private readonly ILogger<MainWindow> _logger;
    private readonly UpdateManager _updateManager;
    private readonly IOptions<StartupOptions> _startupOptions;
    private readonly SolutionExplanation _solutionExplanation = new();

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

    public MainWindow(
        IVelopackAssetValidator assetValidator,
        UpdateManager updateManager,
        IOptions<StartupOptions> startupOptions,
        ILogger<MainWindow> logger)
    {
        _assetValidator = assetValidator;
        _logger = logger;
        _updateManager = updateManager;
        _startupOptions = startupOptions;
        InitializeComponent();

        List<TextBox> cells = GetDescendants<TextBox>(GameGrid);
        _cellLayout = new Lazy<ImmutableList<TextBox>>(() => PopulateCells(cells));
        _solveTask = Task.Factory.StartNew(SolveQueries, TaskCreationOptions.LongRunning);
        _serializer = new GameEngineSerializer();
        int cellSize = 20;
        _ruleFactories =
        [
            new RenbanUiRule(cellSize, "Renban"),
            new GermanWhisperUiRule(cellSize, "German Whisper"),
            new EvenOddCellUiRule(cellSize, "Odd Cell", isEven: false),
            new EvenOddCellUiRule(cellSize, "Even Cell", isEven: true),
            new KropkiDotUiRule(cellSize, "Black Kropki", isDouble: true),
            new KropkiDotUiRule(cellSize, "White Kropki", isDouble: false),
            new PairSumUiRule(cellSize, "Sums"),
            new CageUiRule(cellSize, "Cage"),
            new ThermoUiRule(cellSize, "Thermo"),
            new ModLineUiRule(cellSize, "Mod Thirds"),
            new DivLineUiRule(cellSize, "Div Thirds"),
            new QuadUiRule(cellSize, "Quadruple")
        ];

        if (_startupOptions.Value.LoadFileName is not null)
        {
            _ = LoadGameFromStream(File.OpenRead(_startupOptions.Value.LoadFileName));
        }
        
        AppVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "devel";
    }

    public int VariationValue
    {
        get => (int)GetValue(VariationValueProperty);
        set => SetValue(VariationValueProperty, value);
    }

    public int VariationMin
    {
        get => (int)GetValue(VariationMinProperty);
        set => SetValue(VariationMinProperty, value);
    }

    public int VariationMax
    {
        get => (int)GetValue(VariationMaxProperty);
        set => SetValue(VariationMaxProperty, value);
    }

    public bool VariationAllowed
    {
        get => (bool)GetValue(VariationAllowedProperty);
        set => SetValue(VariationAllowedProperty, value);
    }

    public bool HasVariationDigits
    {
        get => (bool)GetValue(HasVariationDigitsProperty);
        set => SetValue(HasVariationDigitsProperty, value);
    }

    public bool EnableKnightsMove
    {
        get => (bool)GetValue(EnableKnightsMoveProperty);
        set => SetValue(EnableKnightsMoveProperty, value);
    }

    public bool UpdateAvailable
    {
        get => (bool)GetValue(UpdateAvailableProperty);
        set => SetValue(UpdateAvailableProperty, value);
    }

    private ImmutableList<TextBox> CellBoxes => _cellLayout.Value;

    protected override void OnClosing(CancelEventArgs e)
    {
        Application.Current.Shutdown();
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

            cell.Tag = new CellBoxValue(cell.Text, CellStyle.Fixed);
            await ValidateAndReportGameState();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Changing cell caused exception");
        }
    }

    private CancellationTokenSource _cancelPendingEvalutations;
    private async Task ValidateAndReportGameState()
    {
        if (_gameEngine.Rules.IsEmpty) return;

        StatusMessage = "Solving...";
        
        GameState state = BuildGameStateFromUi();

        CancellationTokenSource prevToken = Interlocked.Exchange(ref _cancelPendingEvalutations, new CancellationTokenSource());
        var cancellationToken = _cancelPendingEvalutations.Token;
        prevToken?.Cancel();
        prevToken?.Dispose();

        TaskCompletionSource<GameQueryResult> resultTask = new();
        ISimplificationTracker tracker = _solutionExplanation.IsVisible ? new ToggleableSimplificationTracker() : new NoopTracker();
        _queries.Enqueue(new GameQuery(state, resultTask, cancellationToken, tracker));
        _gameStateAvailable.Trigger();
        
        GameState simplified;
        try
        {
            simplified = await Task.Run(() => _gameEngine.SimplifyState(state, cancellationToken: cancellationToken), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // This version of the game was preempted, just move on
            return;
        }
        
        ImmutableList<TextBox> uiCells = CellBoxes;
        for (int r = 0; r < state.Structure.Rows; r++)
        for (int c = 0; c < state.Structure.Columns; c++)
        {
            TextBox uiCell = uiCells[r * 9 + c];
            CellStyle style = uiCell.Tag is CellBoxValue value ? value.Style : CellStyle.Ambiguous;
            if (style == CellStyle.Fixed) continue;
            CellValueMask mask = simplified.Cells[r, c];
            switch (mask.Count)
            {
                case 0:
                    SetCell(r, c, "", CellStyle.Potential);
                    break;
                case 1:
                    SetCell(r, c, TextFromDigit(mask.GetSingle()), CellStyle.Solved);
                    break;
                default:
                    SetCell(r, c, TextFromDigitMask(mask), CellStyle.Potential);
                    break;
            }
        }
        
        GameQueryResult result;
        try
        {
            result = await resultTask.Task;
        }
        catch (OperationCanceledException)
        {
            // This version of the game was preempted, just move on
            return;
        }
        
        _solutionExplanation.Update(result.Explanation);

        Style s = result.Result switch
        {
            GameResult.Unsolvable => GameGrid.TryFindResource("UnsolvableGame") as Style,
            GameResult.Solved => GameGrid.TryFindResource("SolvedGame") as Style,
            GameResult.MultipleSolutions => GameGrid.TryFindResource("AmbiguousGame") as Style,
            _ => null
        };
        foreach (Border border in GetDescendants<Border>(GameGrid)) border.Style = s;

        StatusMessage = result.Result switch
        {
            GameResult.Solved => "Solved",
            GameResult.Unsolvable => "Unsolvable",
            GameResult.MultipleSolutions => "Multiple Solutions",
        };
        if (result.Result == GameResult.Solved)
        {
            for (int r = 0; r < state.Structure.Rows; r++)
            for (int c = 0; c < state.Structure.Columns; c++)
            {
                TextBox uiCell = uiCells[r * 9 + c];
                CellStyle style = uiCell.Tag is CellBoxValue value ? value.Style : CellStyle.Ambiguous;
                if (style == CellStyle.Fixed) continue;
                switch (result)
                {
                    case UnsolvableQueryResult:
                        SetCell(r, c, "", CellStyle.Potential);
                        break;
                    case SolvedQueryResult solved:
                        SetCell(r, c, TextFromDigit(solved.Solution.Cells.GetSingle(r, c)), CellStyle.Solved);
                        break;
                    case MultipleSolutionsQueryResult multi:
                        CellValueMask mask = (multi.Solution1.Cells[r, c] |
                            multi.Solution2.Cells[r, c]);
                        SetCell(r, c, TextFromDigitMask(mask), mask.IsSingle() ? CellStyle.Solved : CellStyle.Ambiguous);
                        break;
                }
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
            if (uiCells[r * 9 + c].Tag is CellBoxValue { Style: CellStyle.Fixed, Text: { } cellText })
            {
                cells.SetSingle(r, c, DigitFromText(cellText));
            }
        }

        state = state.WithCells(cells.MoveToImmutable());
        return state;
    }

    private static CellValue DigitFromText(string text)
    {
        return new CellValue((ushort)(ushort.TryParse(text, out ushort digit) ? digit - 1 : ~0));
    }

    private static string TextFromDigit(CellValue digit)
    {
        return digit.NumericValue.ToString();
    }

    private static string TextFromDigitMask(CellValueMask digit)
    {
        var b = new StringBuilder(9);
        for (ushort d = 0; d < 9; d++)
            if (digit.Contains(new CellValue(d)))
                b.Append(TextFromDigit(new CellValue(d)));

        return b.ToString();
    }

    [DoesNotReturn]
    [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
    private async Task SolveQueries()
    {
        while (true)
        {
            _exit.Token.ThrowIfCancellationRequested();
            await _gameStateAvailable.WaitAsync();
            while (_queries.TryDequeue(out GameQuery s))
            {
                _logger.LogDebug("Found state to query");
                try
                {
                    GameResult result = _gameEngine.Evaluate(
                        s.State,
                        out GameState? solution,
                        out GameState? conflict,
                        s.Tracker,
                        out ISimplificationChain explanation,
                        s.CancellationToken
                    );
                    _logger.LogDebug("Query complete result={result}", result);
                    GameQueryResult queryResult = result switch
                    {
                        GameResult.Unknown => UnknownQueryResult.Instance,
                        GameResult.Unsolvable => UnsolvableQueryResult.Instance,
                        GameResult.Solved => new SolvedQueryResult(solution.Value),
                        GameResult.MultipleSolutions => new MultipleSolutionsQueryResult(solution.Value, conflict.Value),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    s.OnSolved.TrySetResult(queryResult with {Explanation = explanation});
                }
                catch (OperationCanceledException)
                {
                    s.OnSolved.SetCanceled();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "EXCEPTION IN GAME STATE: {exception}", e.Message);
                }

                _logger.LogDebug("Dispatch query complete");
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private void SetCell(int row, int column, string text, CellStyle style)
    {
        TextBox cell = CellBoxes[row * 9 + column];
        SetCellTextAndStyle(cell, text, style);
        cell.Tag = new CellBoxValue(text, style);
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

        if (box.Tag is CellBoxValue { Style: CellStyle.Fixed }) return;

        SetCellTextAndStyle(box, "", CellStyle.Fixed);
    }

    private void CellUnfocused(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not TextBox box) return;

        if (box.Tag is not CellBoxValue computed) return;

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
    
    private record class GameQueryResult(GameResult Result, ISimplificationChain Explanation = null);
    private record class SolvedQueryResult(GameState Solution) : GameQueryResult(GameResult.Solved);
    private record class MultipleSolutionsQueryResult(GameState Solution1, GameState Solution2) : GameQueryResult(GameResult.MultipleSolutions);

    private record class UnsolvableQueryResult() : GameQueryResult(GameResult.Unsolvable)
    {
        public static readonly UnsolvableQueryResult Instance = new();
    }

    private record class UnknownQueryResult() : GameQueryResult(GameResult.Unknown)
    {
        public static readonly UnknownQueryResult Instance = new();
    }

    private record class GameQuery(
        GameState State,
        TaskCompletionSource<GameQueryResult> OnSolved,
        CancellationToken CancellationToken,
        ISimplificationTracker Tracker
    );

    private readonly record struct CellBoxValue(string Text, CellStyle Style);

    private void CloseWindow(object sender, ExecutedRoutedEventArgs e) => Close();

    private void NewGame(object sender, ExecutedRoutedEventArgs e)
    {
        _cancelPendingEvalutations?.Cancel();
        _gameEngine = GameEngine.Default;
        _selfTriggered++;
        foreach (TextBox cell in CellBoxes)
        {
            cell.Text = "";
        }
        _selfTriggered--;
        _currentRule = null;
        _ruleCollection.Rules.Clear();
        while (RuleDrawingGroup.Children.Count > 2)
        {
            RuleDrawingGroup.Children.RemoveAt(2);
        }
    }

    private async void SaveGame(object sender, ExecutedRoutedEventArgs e)
    {
        try
        {
            var dlg = new SaveFileDialog
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
        
            UpdateGameRules();
            GameState gameState = BuildGameStateFromUi();
            await using Stream stream = dlg.OpenFile();
            await _serializer.SaveGameAsync(_gameEngine.WithInitialState(gameState), stream);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to save game");
            MessageBox.Show(this, $"Failed to save game: \n\nDetails: {ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OpenGame(object sender, ExecutedRoutedEventArgs e)
    {
        try
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
        
            _cancelPendingEvalutations?.Cancel();
            _queries.Clear();
            
            await using Stream stream = dlg.OpenFile();
            await LoadGameFromStream(stream);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to load game");
            MessageBox.Show(this, $"Failed to load game: \n\nDetails: {ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadGameFromStream(Stream stream)
    {
        GameEngine gameEngine = await _serializer.LoadGameAsync(stream);
        _gameEngine = gameEngine;
        for (int r = 0; r < gameEngine.InitialState.Structure.Rows; r++)
        for (int c = 0; c < gameEngine.InitialState.Structure.Columns; c++)
        {
            if (gameEngine.InitialState.Cells[r, c].TryGetSingle(out var x))
            {
                SetCell(r, c, TextFromDigit(x), CellStyle.Fixed);
            }
            else
            {
                SetCell(r, c, "", CellStyle.Potential);
            }
        }

        _ruleCollection.Rules.Clear();
        List<UiGameRule> uiRules = _ruleFactories.SelectMany(f => f.DeserializeRules(gameEngine.Rules)).ToList();
        _ruleCollection.Rules.AddRange(uiRules);
        while (RuleDrawingGroup.Children.Count > 2)
        {
            RuleDrawingGroup.Children.RemoveAt(2);
        }

        foreach (UiGameRule uiRule in uiRules)
        {
            RuleDrawingGroup.Children.Add(uiRule.Drawing);
        }
        await ValidateAndReportGameState();
    }

    private bool _captured;
    private readonly GameRuleCollection _ruleCollection = new();
    private readonly List<UiGameRuleFactory> _ruleFactories;
    private UiGameRule _currentRule;
    private UiGameRuleFactory _currentFactory;

    private async void CellMouseDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (_currentFactory is null)
            {
                return;
            }
        
            Point point = e.GetPosition(RuleDisplayCanvas);
            CellLocation location = _currentFactory.TranslateFromPoint(point);
            bool modified = false;

            if (_ruleCollection.Rules.Where(r => r.Factory == _currentFactory).FirstOrDefault(r => r.TryAddSegment(location, BuildParameters())) is { } ruleModified)
            {
                _currentRule = ruleModified;
                modified = true;

            }
            else
            {
                if (_currentFactory.TryStart(point, BuildParameters(), out UiGameRule rule))
                {
                    RuleDrawingGroup.Children.Add(rule.Drawing);
                    _currentRule = rule;
                    modified = true;
                    _ruleCollection.Rules.Add(_currentRule);
                }
            }

            ((Grid)sender).CaptureMouse();
            e.Handled = true;
            _captured = true;

            if (modified && !_currentFactory.IsContinuous)
            {
                await RunGameEngine();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Error while updating game state: \n\nDetails: {ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private RuleParameters BuildParameters()
    {
        ushort mask = 0;
        foreach (var child in VariationGrid.Children.OfType<CheckBox>())
        {
            mask = (ushort)((mask >> 1) | (child.IsChecked is true ? 1 << 8 : 0));
        }

        return new RuleParameters(mask, (ushort)VariationValue);
    }

    private void CellMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_currentFactory is null)
        {
            return;
        }

        _currentRule?.Complete();
        
        _captured = false;
        ((Grid)sender).ReleaseMouseCapture();
        e.Handled = true;
    }

    private async void CellMouseMove(object sender, MouseEventArgs e)
    {
        try
        {
            if (!_captured || _currentRule is null) return;

            if (_currentRule.TryContinue(e.GetPosition(RuleDisplayCanvas)))
            {
                await RunGameEngine();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Error while updating game state: \n\nDetails: {ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void ChangeRuleType(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not RadioButton radio) return;
        
        if (radio.Content is not string ruleName) return;

        _currentFactory = _ruleFactories.FirstOrDefault(f => string.Equals(f.Name, ruleName, StringComparison.OrdinalIgnoreCase));
        if (_currentFactory?.VariationRange is { } variations)
        {
            (int start, int length) = variations.GetOffsetAndLength(int.MaxValue);
            VariationAllowed = true;
            VariationMin = start;
            VariationMax = start + length;
        }
        else
        {
            VariationAllowed = false;
            VariationMin = 0;
            VariationMax = 0;
        }

        HasVariationDigits = _currentFactory?.HasVariationDigits ?? false;

        _currentRule = null;
    }

    private async void EvaluateGame(object sender, RoutedEventArgs e)
    {
        _solutionExplanation.Show();
        _solutionExplanation.Owner = this;
        try
        {
            await RunGameEngine();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Error while updating game state: \n\nDetails: {ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RunGameEngine()
    {
        UpdateGameRules();
        await ValidateAndReportGameState();
    }

    private void UpdateGameRules()
    {
        ImmutableArray<IGameRule>.Builder rules = ImmutableArray.CreateBuilder<IGameRule>();
        rules.Add(BasicGameRule.Instance);
        rules.AddRange(_ruleFactories.SelectMany(f => f.SerializeRules(_ruleCollection.Rules.Where(r => r.IsValid))));
        if (EnableKnightsMove)
        {
            rules.Add(new KnightsMoveRule());
        }
        _gameEngine = _gameEngine.WithRules(rules.ToImmutable());
    }

    private async void UpdateApp(object sender, RoutedEventArgs e)
    {
        var menuItem = e.OriginalSource as MenuItem;
        try
        {
            StatusMessage = "Updating...";
            IsEnabled = false;
            if (menuItem != null) menuItem.IsEnabled = false;
            GameState gameState = BuildGameStateFromUi();
            string tempFile = Path.GetTempFileName();
            await using Stream stream = File.Create(tempFile);
            Task saveTask = _serializer.SaveGameAsync(_gameEngine.WithInitialState(gameState), stream);
            UpdateInfo updateInfo = await _updateManager.CheckForUpdatesAsync();
            if (updateInfo is null)
            {
                UpdateAvailable = false;
                await saveTask;
                File.Delete(tempFile);
                return;
            }

            await _updateManager.DownloadUpdatesAsync(updateInfo, pct => Dispatcher.Invoke(() => ProgressPercentage = pct));
            _updateManager.ApplyUpdatesAndRestart(null, ["--load", tempFile]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update app.");
            MessageBox.Show(this, "Error while updating app.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            ProgressPercentage = 0;
            IsEnabled = true;
            if (menuItem != null) menuItem.IsEnabled = true;
        }
    }

    private X509Certificate2 GetCurrentSigner()
    {
        try
        {
            // This is the only built in way to check the authenticode signature of a file
#pragma warning disable SYSLIB0057 
            return new X509Certificate2(Assembly.GetEntryAssembly().Location);
#pragma warning restore SYSLIB0057
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    private async void CheckForUpdates(object sender, RoutedEventArgs e)
    {
        var menuItem = e.OriginalSource as MenuItem;
        try
        {
            if (!_updateManager.IsInstalled)
            {
                MessageBox.Show(this, "Application is not installed, so cannot be automatically updated.", "Not Installed", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            StatusMessage = "Checking for updates...";
            if (menuItem != null) menuItem.IsEnabled = false;
            UpdateInfo updateInfo = await _updateManager.CheckForUpdatesAsync();
            if (updateInfo is not null)
            {
                var validation = _assetValidator.Validate(updateInfo);

                if (validation.Code < ValidationResultCode.Trusted)
                {
                    if (MessageBox.Show(
                            "Update warning",
                            "Target asset does not appear to be from the correct source. Continue with the installation?",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning,
                            MessageBoxResult.No
                        ) !=
                        MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                UpdateAvailable = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check update state.");
            MessageBox.Show(this, "Error while checking update state.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (menuItem != null) menuItem.IsEnabled = true;
        }
    }
}