namespace Pigeon.Filtering;

/// <summary>
/// Represents a filter that is executed when right before a message is sent.
/// </summary>
public interface ISendFilter
{
    /// <summary>
    /// Called right before a message is sent.
    /// </summary>
    /// <param name="topicName">The name of the topic to send the message to.</param>
    /// <param name="envelope">The serialized message envelope.</param>
    /// <param name="next">The next filter in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    ValueTask Send(string topicName, SerializedEnvelope envelope, Func<string, SerializedEnvelope, CancellationToken, ValueTask> next, CancellationToken cancellationToken);
}
