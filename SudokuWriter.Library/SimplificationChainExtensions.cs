using System.Runtime.CompilerServices;

namespace VaettirNet.SudokuWriter.Library;

public static class SimplificationChainExtensions
{
    public static void Record(
        this ISimplificationChain chain,
        [InterpolatedStringHandlerArgument(nameof(chain))] SimplificationInterpolationHandler record
    ) =>
        chain.Record(record.Tracker.Record(record));
}