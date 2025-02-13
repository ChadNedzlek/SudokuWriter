using System.Collections.Generic;

namespace VaettirNet.SudokuWriter.Library;

public sealed class NoopTracker : ISimplificationTracker
{
    private readonly ISimplificationChain _emptyChain = new NoopChain();
    public bool IsTracking => false;
    public static NoopTracker Instance { get; } = new();
    public ISimplificationChain GetEmptyChain() => _emptyChain;

    public SimplificationRecord Record(SimplificationInterpolationHandler record) => SimplificationRecord.Empty;

    private class NoopChain : ISimplificationChain
    {
        public void Record(SimplificationRecord record)
        {
        }

        public ISimplificationTracker Tracker => Instance;
        public ISimplificationChain Fork() => this;
        public IEnumerable<SimplificationRecord> GetRecords() => [];
    }

}