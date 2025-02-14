using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mono.Options;
using VaettirNet.SudokuWriter.Library;
using VaettirNet.VelopackExtensions.SignedReleases;
using VaettirNet.VelopackExtensions.SignedReleases.Sources;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace VaettirNet.SudokuWriter.Gui;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private ILogger _logger;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string loadFileOnStartup = null;
        bool deleteLoadedFile = false;
        new OptionSet
            {
                {"load=", "Sudoku file to load on startup", v => loadFileOnStartup = v},
                {"delete-input", "Delete the loaded file after loading", v => deleteLoadedFile = v is not null, true },
            }
            .Parse(e.Args);
        
        ServiceCollection collection = new ServiceCollection();
        collection.AddLogging();
        collection.AddSingleton<ILoggerProvider, BufferedFileLoggerProvider>();
        collection.Configure<BufferedFileLoggerProvider.Options>(o =>
        {
            #if DEBUG
            o.CountOfLogs = 0;
            #else
            o.CountOfLogs = 5;
            #endif
            o.FilePathPattern = Path.Join(
                VelopackLocator.GetDefault(null).RootAppDir
                    ?? Assembly.GetEntryAssembly()?.Location
                    ?? Environment.CurrentDirectory,
                "logs",
                "log_{0}_{1}.txt");
        });
        collection.AddSingleton<IVelopackAssetValidator, SameSignerAsEntryPointValidator>();
        collection.AddVelopackReleaseValidation();
        collection.AddSingleton<IUpdateSource, ValidatingGitHubReleaseSource>();
        collection.Configure<ValidatingGitHubReleaseSource.Options>(
            o =>
            {
                o.RepoUrl = "https://github.com/ChadNedzlek/SudokuWriter";
            }
        );
        collection.Configure<UpdateOptions>(_ => { });
        collection.AddSingleton(
            s => new UpdateManager(
                s.GetRequiredService<IUpdateSource>(),
                s.GetRequiredService<IOptions<UpdateOptions>>().Value,
                s.GetRequiredService<ILogger<UpdateManager>>(),
                s.GetService<IVelopackLocator>()
            )
        );
        collection.AddOptions<StartupOptions>();
        collection.Configure<StartupOptions>(o =>
            {
                o.LoadFileName = loadFileOnStartup;
                o.DeleteFile = deleteLoadedFile;
            }
        );
        collection.AddSingleton(VelopackApp.Build().SetArgs(e.Args));
        _serviceProvider = collection.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        var velopackApp = _serviceProvider.GetRequiredService<VelopackApp>();
        velopackApp.Run(_serviceProvider.GetRequiredService<ILogger<VelopackApp>>());
        var win = ActivatorUtilities.CreateInstance<MainWindow>(_serviceProvider);
        win.Show();
        MainWindow = win;
        DispatcherUnhandledException += HandleException;
    }

    private void HandleException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (_logger == null) return;
        
        _logger.LogCritical(e.Exception, "Exception unhandled by dispatcher: {message}", e.Exception.Message);
        e.Handled = true;

    }

    private ServiceProvider _serviceProvider;
    public IServiceProvider ServiceProvider => _serviceProvider;

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider.Dispose();
        base.OnExit(e);
    }
}