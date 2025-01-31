using System;
using JetBrains.Annotations;
using Mono.Options;

namespace VaettirNet.BuildTools;

internal static class WriteExtensions
{
    [StringFormatMethod(nameof(format))]
    public static void Write(
        this CommandSet commandSet,
        VerbosityLevel level,
        bool writeToError,
        ConsoleColor? color,
        string format,
        params object[] args
    )
    {
        if (Program.Verbosity < level) return;

        var writer = writeToError ? commandSet.Error : commandSet.Out;
        if (color is {} c)
        {
            Console.ForegroundColor = c;
        }
        
        writer.WriteLine(format, args);

        if (color is { })
        {
            Console.ResetColor();
        }
    }

    [StringFormatMethod(nameof(format))]
    public static void WriteVerbose(this CommandSet commandSet, ConsoleColor color, string format, params object[] args)
        => Write(commandSet, VerbosityLevel.Verbose, false, color, format, args);
    
    [StringFormatMethod(nameof(format))]
    public static void WriteVerbose(this CommandSet commandSet,string format, params object[] args)
        => Write(commandSet, VerbosityLevel.Verbose, false, null, format, args);
    
    [StringFormatMethod(nameof(format))]
    public static void WriteError(this CommandSet commandSet, ConsoleColor? color, string format, params object[] args)
        => Write(commandSet, VerbosityLevel.Error, true, color, format, args);
    
    [StringFormatMethod(nameof(format))]
    public static void WriteError(this CommandSet commandSet, string format, params object[] args)
        => Write(commandSet, VerbosityLevel.Error, true, ConsoleColor.Red, format, args);
    
    [StringFormatMethod(nameof(format))]
    public static void WriteWarning(this CommandSet commandSet, ConsoleColor? color, string format, params object[] args)
        => Write(commandSet, VerbosityLevel.Warning, true, color, format, args);
    
    [StringFormatMethod(nameof(format))]
    public static void WriteWarning(this CommandSet commandSet, string format, params object[] args)
        => Write(commandSet, VerbosityLevel.Warning, true, ConsoleColor.Yellow, format, args);
    
    [StringFormatMethod(nameof(format))]
    public static void Write(this CommandSet commandSet, ConsoleColor color, string format, params object[] args)
        => Write(commandSet, VerbosityLevel.Normal, false, color, format, args);
    
    [StringFormatMethod(nameof(format))]
    public static void Write(this CommandSet commandSet, string format, params object[] args)
        => Write(commandSet, VerbosityLevel.Normal, false, null, format, args);
    
    [StringFormatMethod(nameof(format))]
    public static void WriteImportant(this CommandSet commandSet, ConsoleColor color, string format, params object[] args)
        => Write(commandSet, VerbosityLevel.Quiet, false, color, format, args);
    
    [StringFormatMethod(nameof(format))]
    public static void WriteImportant(this CommandSet commandSet, string format, params object[] args)
        => Write(commandSet, VerbosityLevel.Quiet, false, null, format, args);
}