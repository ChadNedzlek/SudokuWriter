using System.Collections.Generic;

namespace VaettirNet.SudokuWriter.Library;

public interface ISimplificationChain
{
    void Record(SimplificationRecord record);
    ISimplificationTracker Tracker { get; }
    ISimplificationChain Fork();
    IEnumerable<SimplificationRecord> GetRecords();
}