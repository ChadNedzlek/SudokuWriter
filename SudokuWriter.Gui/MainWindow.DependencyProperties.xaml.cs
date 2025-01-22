using System.Windows;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace SudokuWriter.Gui;

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
        new PropertyMetadata(default(bool)));
}