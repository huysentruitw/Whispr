namespace Whispr.Bus;

/// <summary>
/// Interface for the message sender that sends messages to the transport.
/// </summary>
public interface IMessageSender
{
    /// <summary>
    /// Sends a message to the transport.
    /// </summary>
    /// <param name="topicName">The destination topic name.</param>
    /// <param name="envelope">The serialized message envelope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask Send(string topicName, SerializedEnvelope envelope, CancellationToken cancellationToken);
}
