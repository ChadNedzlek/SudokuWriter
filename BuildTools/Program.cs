using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mono.Options;

namespace VaettirNet.BuildTools;

internal static class Program
{
    public const byte CaSlot = 0x84;
    static int Main(string[] args)
    {
        ServiceCollection collection = new ServiceCollection();

        collection.AddLogging();

        using var services = collection.BuildServiceProvider();

        ILogger logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");

        var commands = new CommandSet(Environment.GetCommandLineArgs()[0])
        {
            ActivatorUtilities.CreateInstance<GenerateManifestCommand>(services),
            ActivatorUtilities.CreateInstance<VerifyManifestCommand>(services),
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