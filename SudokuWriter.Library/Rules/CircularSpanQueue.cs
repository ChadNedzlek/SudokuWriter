using System;

namespace VaettirNet.SudokuWriter.Library.Rules;

public ref struct CircularSpanQueue<T>
{
    private readonly Span<T> _span;
    private int _head = -1;
    private int _tail;

    public CircularSpanQueue(Span<T> span)
    {
        _span = span;
    }

    public void Enqueue(T value)
    {
        if (_head < 0)
        {
            _span[_tail] = value;
            _head = _tail;
            _tail++;
            return;
        }

        if (_tail == _head)
            throw new InvalidOperationException("Buffer full");

        if (_tail > _head)
        {
            _span[_tail++] = value;
            if (_tail == _span.Length)
                _tail = 0;
            return;
        }

        _span[_tail++] = value;
    }

    public bool TryDequeue(out T value)
    {
        if (_head == -1)
        {
            value = default;
            return false;
        }

        value = _span[_head++];
        if (_head >= _span.Length)
        {
            _head = 0;
        }

        if (_head == _tail)
        {
            _head = -1;
            _tail = 0;
        }

        return true;
    }
}