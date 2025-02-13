using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace VaettirNet.SudokuWriter.Library;

public sealed class ToggleableSimplificationTracker : ISimplificationTracker
{
    private class Chain : ISimplificationChain
    {
        private ImmutableList<SimplificationRecord> _records;

        public Chain(ISimplificationTracker tracker) : this(ImmutableList<SimplificationRecord>.Empty, tracker)
        {
        }

        private Chain(ImmutableList<SimplificationRecord> records, ISimplificationTracker tracker)
        {
            _records = records;
            Tracker = tracker;
        }

        public void Record(SimplificationRecord record)
        {
            if (Tracker.IsTracking)
            {
                _records = _records.Add(record);
            }
        }

        public ISimplificationTracker Tracker { get; }
        
        public ISimplificationChain Fork()
        {
            if (!Tracker.IsTracking)
            {
                return this;
            }
            
            return new Chain(_records, Tracker);
        }

        public IEnumerable<SimplificationRecord> GetRecords() => _records;
    }

    public bool IsTracking { get; set; } = true;
    
    public ISimplificationChain GetEmptyChain() => new Chain(this);

    public SimplificationRecord Record(SimplificationInterpolationHandler record)
    {
        return IsTracking ? record.Build() : SimplificationRecord.Empty;
    }
}