using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Mono.Options;

namespace VaettirNet.BuildTools;

public abstract class CommandBase : Command
{
    public bool ShowHelp { get; private set; }
    
    protected CommandBase(string name, string help = null) : base(name, help)
    {
        Options ??= new OptionSet();
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Add("help|h|?", "Show help commands", v => ShowHelp = v is not null, true);
        List<string> extra = Options.Parse(arguments);
        IList<string> handled = HandleExtraArgs(extra);
        if (handled.Count > 0)
        {
            CommandSet.WriteError($"Unknown argument '{handled[0]}'");
            Options.WriteOptionDescriptions(Console.Error);
            return 1;
        }

        if (ShowHelp)
        {
            Options.WriteOptionDescriptions(CommandSet.Error);
            return 1;
        }

        try
        {
            return Execute();
        }
        catch (MissingRequiredArgumentException e)
        {
            CommandSet.WriteError(e.Message);
            Options.WriteOptionDescriptions(CommandSet.Error);
            return e.ExitCode ?? 1;
        }
    }

    public virtual IList<string> HandleExtraArgs(IList<string> arguments) => arguments;

    protected abstract int Execute();

    [ContractAnnotation("value:null => halt")]
    protected void ValidateRequiredArgument<T>(T value, string argumentName)
    {
        if (typeof(T) == typeof(string))
        {
            if (string.IsNullOrEmpty(Unsafe.As<string>(value)))
            {
                throw new MissingRequiredArgumentException(argumentName);
            }
        }

        if (typeof(T).IsEnum && Convert.ToInt32(value) == 0)
        {
            throw new MissingRequiredArgumentException(argumentName);
        }

        if (value is null)
        {
            throw new MissingRequiredArgumentException(argumentName);
        }
    }
}