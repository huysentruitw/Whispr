using System.Collections.Concurrent;

namespace Whispr.AzureServiceBus.Transport;

internal sealed class PendingCompletions(int maxPending)
{
    private readonly SemaphoreSlim _slots = new(maxPending, maxPending);
    private readonly ConcurrentDictionary<Task, byte> _pending = new();

    public int Count => _pending.Count;

    public async ValueTask Track(Func<Task> complete)
    {
        // A full tracker completes inline, so it is never slower than awaiting every completion
        if (!_slots.Wait(0))
        {
            await complete();
            return;
        }

        Task task;
        try
        {
            task = complete();
        }
        catch
        {
            _slots.Release();
            throw;
        }

        _pending[task] = 0;
        _ = task.ContinueWith(
            static (completed, state) => ((PendingCompletions)state!).OnCompleted(completed),
            this,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    public async ValueTask Drain(CancellationToken cancellationToken)
    {
        while (!_pending.IsEmpty)
        {
            try
            {
                await Task.WhenAll(_pending.Keys).WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch
            {
                // The completion handles its own failure, draining only waits for it
            }

            // A finished completion leaves the set in its continuation, which may not have run yet
            await Task.Yield();
        }
    }

    private void OnCompleted(Task completed)
    {
        try
        {
            _ = completed.Exception;
            _pending.TryRemove(completed, out _);
        }
        finally
        {
            _slots.Release();
        }
    }
}
