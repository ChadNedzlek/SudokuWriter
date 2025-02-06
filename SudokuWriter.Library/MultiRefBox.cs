using System;

namespace VaettirNet.SudokuWriter.Library;

public readonly struct MultiRefBox<T>
{
    private readonly OffsetList _offsets;
    private readonly int _length;

    internal MultiRefBox(OffsetList offsets, int length)
    {
        _offsets = offsets;
        _length = length;
    }

    public MultiRef<T> Unbox(Span<T> references) => new(references, _offsets, _length);
    public ReadOnlyMultiRef<T> Unbox(ReadOnlySpan<T> references) => new(references, _offsets, _length);
}