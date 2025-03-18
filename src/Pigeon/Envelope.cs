namespace Pigeon;

/// <summary>
/// Represents the message envelope containing the message and metadata.
/// </summary>
/// <typeparam name="TMessage">The type of the message.</typeparam>
public sealed record Envelope<TMessage>
    where TMessage : class
{
    /// <summary>
    /// The message ID.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// The message.
    /// </summary>
    public required TMessage Message { get; init; }

    /// <summary>
    /// The message type.
    /// </summary>
    public required string MessageType { get; init; }

    /// <summary>
    /// The message headers.
    /// </summary>
    public required Dictionary<string, string> Headers { get; init; }

    /// <summary>
    /// The destination topic name.
    /// </summary>
    public required string DestinationTopicName { get; init; }

    /// <summary>
    /// The correlation ID.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// The deferred until date and time. If <see langword="null"/>, the message is not deferred and will be processed immediately.
    /// </summary>
    public required DateTimeOffset? DeferredUntil { get; init; }
}
