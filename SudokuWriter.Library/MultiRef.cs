using System;
using System.Collections.Generic;

namespace SudokuWriter.Library;

public readonly ref struct MultiRef<T>
{
    private readonly Span<T> _ref;
    private readonly List<int> _offsets = new(10);

    public MultiRef(Span<T> validSpace)
    {
        _ref = validSpace;
    }

    public void Include(ref T target)
    {
        if (!_ref.Overlaps(new ReadOnlySpan<T>(ref target), out int offset))
        {
            throw new ArgumentException("target is not contained in this MultiRef", nameof(target));
        }

        _offsets.Add(offset);
    }
    
    public void Include(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _ref.Length);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        _offsets.Add(index);
    }

    public void ForEach(RefAction<T> callback)
    {
        foreach (int ptr in _offsets)
        {
            callback(ref _ref[ptr]);
        }
    }

    public int Count => _offsets.Count;

    public ref T this[int index] => ref _ref[index];

    public TOut Aggregate<TOut>(RefAggregator<T, TOut> callback) => Aggregate(default, callback);

    public TOut Aggregate<TOut>(TOut seed, RefAggregator<T, TOut> callback)
    {
        foreach (int ptr in _offsets)
        {
            seed = callback(seed, ref _ref[ptr]);
        }

        return seed;
    }
}

public delegate void RefAction<T>(scoped ref T value);
public delegate TOut RefAggregator<TIn, TOut>(TOut input, scoped ref TIn value);