using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mono.Options;
using VaettirNet.BuildTools.Commands.Manifest;
using VaettirNet.BuildTools.Commands.Release;
using VaettirNet.VelopackExtensions.SignedReleases;

namespace VaettirNet.BuildTools;

public enum VerbosityLevel
{
    Silent,
    Error,
    Warning,
    Quiet,
    Normal,
    Verbose,
}

internal static class Program
{
    public const byte CaSlot = 0x84;

    public static VerbosityLevel Verbosity { get; private set; } = VerbosityLevel.Normal;
    
    static int Main(string[] args)
    {
        ServiceCollection collection = new ServiceCollection();

        collection.AddLogging();
        collection.AddSingleton<ReleaseSignerFactory>();
        collection.AddVelopackReleaseValidation();

        using var services = collection.BuildServiceProvider();

        ILogger logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");

        var commands = new CommandSet(Environment.GetCommandLineArgs()[0])
        {
            ActivatorUtilities.CreateInstance<GenerateManifestCommand>(services),
            ActivatorUtilities.CreateInstance<VerifyManifestCommand>(services),
            ActivatorUtilities.CreateInstance<VerifyReleaseCommand>(services),
            ActivatorUtilities.CreateInstance<SignReleaseCommand>(services),
            ActivatorUtilities.CreateInstance<SignGithubRelease>(services),
            {"verbose|v", "Increase verbosity level", v => Verbosity += v is null ? -1 : 1},
            {"quiet|q", "Decrease verbosity level", v => Verbosity -= v is null ? -1 : 1},
        };

        try
        {
            return commands.Run(args);
        }
        catch (CommandFailedException e)
        {
            logger.LogWarning("Command failed with exit code '{exitCode}' message: {message}", e.ExitCode, e.Message);
            Console.Error.WriteLine(e);
            if (e.ExitCode is { } ex) return ex;
            return 1;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Unexpected exception encountered. Terminating");
            logger.LogCritical(e, "Unhandled exception");
#if DEBUG
                throw;
#else
            return 1000;
#endif
        }
    }
}