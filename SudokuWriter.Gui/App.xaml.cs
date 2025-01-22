using System.Windows;
using System.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Velopack;

namespace SudokuWriter.Gui;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        VelopackApp.Build().Run();
        ServiceCollection collection = new ServiceCollection();
        collection.AddLogging();
        _serviceProvider = collection.BuildServiceProvider();
        var win = ActivatorUtilities.CreateInstance<MainWindow>(_serviceProvider);
        win.Show();
        MainWindow = win;
    }

    private ServiceProvider _serviceProvider;
    public IServiceProvider ServiceProvider => _serviceProvider;

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider.Dispose();
        base.OnExit(e);
    }
}