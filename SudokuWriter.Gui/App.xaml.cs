using System.Windows;
using System.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mono.Options;
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

        string loadFileOnStartup = null;
        new OptionSet
            {
                {"load=", "Sudoku file to load on startup", v => loadFileOnStartup = v},
            }
            .Parse(e.Args);
        
        ServiceCollection collection = new ServiceCollection();
        collection.AddLogging();
        collection.AddSingleton(
            s => new UpdateManager(
                "https://github.com/ChadNedzlek/SudokuWriter",
                logger: s.GetRequiredService<ILogger<UpdateManager>>()
            )
        );
        collection.AddOptions<StartupOptions>();
        collection.Configure<StartupOptions>(o => o.LoadFileName = loadFileOnStartup);
        collection.AddSingleton(VelopackApp.Build().SetArgs(e.Args));
        _serviceProvider = collection.BuildServiceProvider();
        _serviceProvider.GetRequiredService<VelopackApp>().Run(_serviceProvider.GetRequiredService<ILogger<VelopackApp>>());
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