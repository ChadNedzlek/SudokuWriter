using System.Threading;
using System.Threading.Tasks;

namespace VaettirNet.SudokuWriter.Library;

public class AutoResetEventAsync
{
    private TaskCompletionSource _current = new();

    public Task WaitAsync() => _current.Task;

    public void Trigger() => Interlocked.Exchange(ref _current, new ()).SetResult();
}