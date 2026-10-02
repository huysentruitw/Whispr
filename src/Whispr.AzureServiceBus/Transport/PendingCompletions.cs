using System.Collections.Concurrent;

namespace Whispr.AzureServiceBus.Transport;

internal sealed class PendingCompletions(int maxPending, Action<Exception, string> onFailed)
{
    private readonly SemaphoreSlim _slots = new(maxPending, maxPending);
    private readonly ConcurrentDictionary<Task, string> _pending = new();

    public int Count => _pending.Count;

    public async ValueTask Track(Func<Task> complete, string messageId)
    {
        await _slots.WaitAsync();

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

        _pending[task] = messageId;
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
                // A failed completion is reported by OnCompleted, draining only waits for it
            }
        }
    }

    private void OnCompleted(Task completed)
    {
        _pending.TryRemove(completed, out var messageId);

        if (completed.Exception is { } exception)
            onFailed(exception.GetBaseException(), messageId ?? string.Empty);

        _slots.Release();
    }
}
