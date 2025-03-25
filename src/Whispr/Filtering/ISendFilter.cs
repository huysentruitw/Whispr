namespace Whispr.Filtering;

/// <summary>
/// Represents a filter that is executed before a message is sent to the transport.
/// </summary>
public interface ISendFilter
{
    /// <summary>
    /// Called before a message is sent to the transport.
    /// </summary>
    /// <param name="topicName">The destination topic name.</param>
    /// <param name="envelope">The serialized message envelope.</param>
    /// <param name="next">The next filter in the pipeline.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask Send(string topicName, SerializedEnvelope envelope, Func<string, SerializedEnvelope, CancellationToken, ValueTask> next, CancellationToken cancellationToken);
}
