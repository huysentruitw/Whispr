using System.Collections.Concurrent;

namespace Whispr.Transport;

internal sealed class InMemoryTransport(ILogger<InMemoryTransport> logger) : ITransport
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

    public ValueTask Send(string topicName, SerializedEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (!_listeners.TryGetValue(topicName, out var callbacks))
            return ValueTask.CompletedTask; // No listeners for this topic

        // Execute callbacks from a fire-and-forget task to simulate async message bus behavior
        foreach (var callback in callbacks)
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
                        logger.LogError(
                            ex,
                            "Failed to process message with ID {MessageId} and correlation ID {CorrelationId}",
                            envelope.MessageId,
                            envelope.CorrelationId);
                    }
                },
                CancellationToken.None);
        }

        return ValueTask.CompletedTask;
    }
}
