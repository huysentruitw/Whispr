using System.Collections.Concurrent;

namespace Whispr.Transport;

internal sealed class InMemoryTransport(ILogger<InMemoryTransport> logger) : ITransport
{
    private readonly ConcurrentDictionary<string, List<Func<SerializedEnvelope, CancellationToken, ValueTask>>>
        _listeners = new ConcurrentDictionary<string, List<Func<SerializedEnvelope, CancellationToken, ValueTask>>>();

    public ValueTask StartListener(
        string queueName,
        string[] topicNames,
        Func<SerializedEnvelope, CancellationToken, ValueTask> messageCallback,
        CancellationToken cancellationToken = default)
    {
        foreach (var topicName in topicNames)
        {
            _listeners.AddOrUpdate(
                topicName,
                _ => [messageCallback],
                (_, existingCallbacks) =>
                {
                    existingCallbacks.Add(messageCallback);
                    return existingCallbacks;
                });
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask Send(string topicName, SerializedEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (!_listeners.TryGetValue(topicName, out var callbacks) || callbacks.Count == 0)
            return ValueTask.CompletedTask;

        foreach (var callback in callbacks)
        {
            FireAndForgetCallback(callback, envelope, cancellationToken);
        }

        return ValueTask.CompletedTask;
    }

    private void FireAndForgetCallback(
        Func<SerializedEnvelope, CancellationToken, ValueTask> callback,
        SerializedEnvelope envelope,
        CancellationToken cancellationToken)
    {
        Task.Run(
            async () =>
            {
                try
                {
                    await callback(envelope, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in message handler");
                }
            },
            cancellationToken);
    }
}