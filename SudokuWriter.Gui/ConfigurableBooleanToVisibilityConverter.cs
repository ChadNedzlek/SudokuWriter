using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SudokuWriter.Gui;

public class ConfigurableBooleanToVisibilityConverter : DependencyObject, IValueConverter
{
    public static readonly DependencyProperty TrueVisibilityProperty = DependencyProperty.Register(
        nameof(TrueVisibility),
        typeof(Visibility),
        typeof(ConfigurableBooleanToVisibilityConverter),
        new PropertyMetadata(Visibility.Visible));

    public Visibility TrueVisibility
    {
        get => (Visibility)GetValue(TrueVisibilityProperty);
        set => SetValue(TrueVisibilityProperty, value);
    }

    public static readonly DependencyProperty FalseVisibilityProperty = DependencyProperty.Register(
        nameof(FalseVisibility),
        typeof(Visibility),
        typeof(ConfigurableBooleanToVisibilityConverter),
        new PropertyMetadata(Visibility.Collapsed));

    public Visibility FalseVisibility
    {
        get => (Visibility)GetValue(FalseVisibilityProperty);
        set => SetValue(FalseVisibilityProperty, value);
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool visible = value switch
        {
            string s => bool.Parse(s),
            bool b => b,
            _ => false,
        };

        bool invert = parameter switch
        {
            string s => bool.Parse(s),
            bool b => b,
            _ => false,
        };

        return visible ^ invert ? TrueVisibility : FalseVisibility;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter switch
        {
            string s => bool.Parse(s),
            bool b => b,
            _ => false,
        };
        
        bool visible = value switch
        {
            Visibility v when v == TrueVisibility => true,
            _ => false,
        };

        return visible ^ invert;
    }
}