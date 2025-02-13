using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace VaettirNet.SudokuWriter.Library;

[InterpolatedStringHandler]
public struct SimplificationInterpolationHandler
{
    private List<object> _parts;

    private void AddPart(object o)
    {
        if (!Tracker.IsTracking) return;
        
        (_parts ??= new List<object>()).Add(o);
    }

    
    public SimplificationInterpolationHandler(int literalLength, int formattedCount, ISimplificationTracker tracker)
    {
        Tracker = tracker;
        _parts = tracker.IsTracking ? new List<object>(formattedCount * 2 + 1) : null;
    }
    
    public SimplificationInterpolationHandler(int literalLength, int formattedCount, ISimplificationChain tracker) : this(literalLength, formattedCount, tracker.Tracker)
    {
    }

    public ISimplificationTracker Tracker { get; set; }

    public void AppendLiteral(string s)
    {
        AddPart(s);
    }

    public void AppendFormatted<T>(T value)
    {
        AddPart(value);
    }
    
    public void AppendFormatted(ReadOnlyMultiRef<CellValueMask> value)
    {
        if (!Tracker.IsTracking) return;
        
        (_parts ??= new List<object>()).Add(value.Render());
    }

    public SimplificationRecord Build()
    {
        if (!Tracker.IsTracking)
        {
            return SimplificationRecord.Empty;
        }

        return new SimplificationRecord(string.Join("", _parts));
    }
}