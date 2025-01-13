using System.Windows;
using System.Windows.Controls;

namespace SudokuWriter.Gui;

public partial class MainWindow : Window
{
    private readonly List<TextBox> _gridCells;

    public MainWindow()
    {
        InitializeComponent();

        _gridCells =GetDescendants<TextBox>(GameGrid); 
    }

    private async void CellChanged(object sender, TextChangedEventArgs e)
    {
        var other = _gridCells.OrderBy(t => t.TransformToAncestor(GameGrid).Transform(default).Y).ThenBy(t => t.TransformToAncestor(GameGrid).Transform(default).X).ToList();
        for (int i = 0; i < 26; i++)
        {
            other[i].Text = ((char)('A' + i)).ToString();
        }
    }

    private static List<T> GetDescendants<T>(FrameworkElement ctrl) where T:FrameworkElement
    {
        List<T> list = [];
        Queue<FrameworkElement> search = new();
        search.Enqueue(ctrl);
        while (search.TryDequeue(out var e))
        {
            foreach (object child in LogicalTreeHelper.GetChildren(e))
            {
                if (child is FrameworkElement fe)
                {
                    if (fe is T t)
                    {
                        list.Add(t);
                    }
                    search.Enqueue(fe);
                }
            }
        }

        return list;
    }
}