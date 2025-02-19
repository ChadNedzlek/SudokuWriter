using System;

namespace VaettirNet.BuildTools;

public class CommandFailedException : Exception
{
    public int? ExitCode { get; }

    public CommandFailedException(string message) : base(message)
    {
    }

    public CommandFailedException(string message, int exitCode) : this(message)
    {
        ExitCode = exitCode;
    }
}