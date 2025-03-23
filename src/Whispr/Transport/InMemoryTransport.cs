using System.Collections.Concurrent;

namespace Whispr.Transport;

internal sealed class InMemoryTransport : ITransport
{
    private readonly ConcurrentDictionary<string, List<Func<SerializedEnvelope, CancellationToken, ValueTask>>> _listeners = new();

    public ValueTask StartListener(string queueName, string[] topicNames, Func<SerializedEnvelope, CancellationToken, ValueTask> messageCallback, CancellationToken cancellationToken = default)
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

    public async ValueTask Send(string topicName, SerializedEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (!_listeners.TryGetValue(topicName, out var callbacks))
            return; // No listeners for this topic

        foreach (var callback in callbacks)
            await callback(envelope, cancellationToken);
    }
}
