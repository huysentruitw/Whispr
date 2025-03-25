namespace Whispr.Outbox;

/// <summary>
/// Represents an outbox.
/// </summary>
public interface IOutbox : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Adds a message to the outbox.
    /// </summary>
    /// <param name="topicName">The name of the topic to send the message to.</param>
    /// <param name="envelope">The serialized message envelope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask Add(string topicName, SerializedEnvelope envelope, CancellationToken cancellationToken = default);
}
