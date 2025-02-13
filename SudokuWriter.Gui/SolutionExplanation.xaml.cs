using System.Collections.ObjectModel;
using System.Windows;
using VaettirNet.SudokuWriter.Library;

namespace VaettirNet.SudokuWriter.Gui;

public partial class SolutionExplanation : Window
{
    public static readonly DependencyProperty RecordsProperty = DependencyProperty.Register(
        nameof(Records),
        typeof(ObservableCollection<SimplificationRecord>),
        typeof(SolutionExplanation),
        new PropertyMetadata(default(ObservableCollection<SimplificationRecord>))
    );

    public ObservableCollection<SimplificationRecord> Records
    {
        get => (ObservableCollection<SimplificationRecord>)GetValue(RecordsProperty);
        set => SetValue(RecordsProperty, value);
    }
    
    public SolutionExplanation()
    {
        InitializeComponent();
    }

    public void Update(ISimplificationChain explanation)
    {
        Dispatcher.Invoke(
            () =>
            {
                Records = new ObservableCollection<SimplificationRecord>(explanation.GetRecords());
            });
    }
}