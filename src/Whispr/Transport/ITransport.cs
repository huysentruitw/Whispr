namespace Whispr.Transport;

/// <summary>
/// Represents a transport that sends and receives messages.
/// </summary>
public interface ITransport
{
    /// <summary>
    /// Sends a message.
    /// </summary>
    /// <param name="topicName">The name of the topic to send the message to.</param>
    /// <param name="envelope">The serialized message envelope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask Send(
        string topicName,
        SerializedEnvelope envelope,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts listening for messages on the specified queue.
    /// </summary>
    /// <param name="queueName">The name of the queue to listen on.</param>
    /// <param name="topicNames">The names of the topics to subscribe to.</param>
    /// <param name="messageCallback">The callback that is invoked when a message is received on the queue.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask StartListener(
        string queueName,
        string[] topicNames,
        Func<SerializedEnvelope, CancellationToken, ValueTask> messageCallback,
        CancellationToken cancellationToken = default);
}
