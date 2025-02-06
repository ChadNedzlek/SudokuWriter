using System.Windows;

namespace VaettirNet.SudokuWriter.Gui;

public partial class MainWindow
{
    public static readonly DependencyProperty EnableKnightsMoveProperty = DependencyProperty.Register(
        nameof(EnableKnightsMove),
        typeof(bool),
        typeof(MainWindow),
        new PropertyMetadata(default(bool))
    );

    public static readonly DependencyProperty VariationValueProperty = DependencyProperty.Register(
        nameof(VariationValue),
        typeof(int),
        typeof(MainWindow),
        new PropertyMetadata(10)
    );

    public static readonly DependencyProperty VariationMinProperty = DependencyProperty.Register(
        nameof(VariationMin),
        typeof(int),
        typeof(MainWindow),
        new PropertyMetadata(0)
    );

    public static readonly DependencyProperty VariationMaxProperty = DependencyProperty.Register(
        nameof(VariationMax),
        typeof(int),
        typeof(MainWindow),
        new PropertyMetadata(45)
    );

    public static readonly DependencyProperty VariationAllowedProperty = DependencyProperty.Register(
        nameof(VariationAllowed),
        typeof(bool),
        typeof(MainWindow),
        new PropertyMetadata(default(bool))
    );

    public static readonly DependencyProperty HasVariationDigitsProperty = DependencyProperty.Register(
        nameof(HasVariationDigits),
        typeof(bool),
        typeof(MainWindow),
        new PropertyMetadata(default(bool))
    );

    public static readonly DependencyProperty UpdateAvailableProperty = DependencyProperty.Register(
        nameof(UpdateAvailable),
        typeof(bool),
        typeof(MainWindow),
        new PropertyMetadata(default(bool))
    );
    
    public static readonly DependencyProperty StatusMessageProperty = DependencyProperty.Register(
        nameof(StatusMessage),
        typeof(string),
        typeof(MainWindow),
        new PropertyMetadata(default(string))
    );

    public string StatusMessage
    {
        get { return (string)GetValue(StatusMessageProperty); }
        set { SetValue(StatusMessageProperty, value); }
    }

    public static readonly DependencyProperty ProgressPercentageProperty = DependencyProperty.Register(
        nameof(ProgressPercentage),
        typeof(int),
        typeof(MainWindow),
        new PropertyMetadata(default(int))
    );

    public int ProgressPercentage
    {
        get { return (int)GetValue(ProgressPercentageProperty); }
        set { SetValue(ProgressPercentageProperty, value); }
    }

    public static readonly DependencyProperty ShowProgressBarProperty = DependencyProperty.Register(
        nameof(ShowProgressBar),
        typeof(bool),
        typeof(MainWindow),
        new PropertyMetadata(default(bool))
    );

    public bool ShowProgressBar
    {
        get { return (bool)GetValue(ShowProgressBarProperty); }
        set { SetValue(ShowProgressBarProperty, value); }
    }
}