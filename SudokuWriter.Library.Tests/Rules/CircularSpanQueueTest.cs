using NUnit.Framework;
using Shouldly;
using VaettirNet.SudokuWriter.Library.Rules;

namespace VaettirNet.SudokuWriter.Library.Tests.Rules;

[TestFixture]
[TestOf(typeof(CircularSpanQueue<>))]
public class CircularSpanQueueTest
{

    [Test]
    public void AddRemoveOneHundredSinglySucceeds()
    {
        CircularSpanQueue<int> q = new CircularSpanQueue<int>(stackalloc int[5]);
        for (int i = 0; i < 100; i++)
        {
            q.Enqueue(i);
            q.TryDequeue(out var check).ShouldBeTrue();
            check.ShouldBe(i);
        }
    }
    
    [Test]
    public void AddRemoveOneHundredSinglyWithBufferSucceeds()
    {
        CircularSpanQueue<int> q = new CircularSpanQueue<int>(stackalloc int[5]);
        q.Enqueue(-3);
        q.Enqueue(-2);
        q.Enqueue(-1);
        for (int i = 0; i < 100; i++)
        {
            q.Enqueue(i);
            q.TryDequeue(out var check).ShouldBeTrue();
            check.ShouldBe(i - 3);
        }
    }
}