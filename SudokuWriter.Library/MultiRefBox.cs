using System;
using System.Collections.Generic;

namespace VaettirNet.SudokuWriter.Library;

public readonly struct MultiRefBox<T>
{
    private readonly List<ulong> _offsets;

    public MultiRefBox(List<ulong> offsets)
    {
        _offsets = offsets;
    }

    public MultiRef<T> Unbox(Span<T> references) => new(references, _offsets);
    public ReadOnlyMultiRef<T> Unbox(ReadOnlySpan<T> references) => new(references, _offsets);
}