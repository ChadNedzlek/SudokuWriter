using System.Runtime.CompilerServices;

namespace VaettirNet.SudokuWriter.Library;

public interface ISimplificationTracker
{
    bool IsTracking { get; }

    ISimplificationChain GetEmptyChain();
    SimplificationRecord Record([InterpolatedStringHandlerArgument("")] SimplificationInterpolationHandler record);
}