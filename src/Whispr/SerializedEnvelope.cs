namespace Whispr;

/// <summary>
/// Serialized envelope.
/// </summary>
public sealed record SerializedEnvelope
{
    /// <summary>
    /// The message body.
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// The message type.
    /// </summary>
    public required string MessageType { get; init; }

    /// <summary>
    /// The message ID.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// The correlation ID.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// The deferred until date and time. If <see langword="null"/>, the message is not deferred and will be processed immediately.
    /// </summary>
    public required DateTimeOffset? DeferredUntil { get; init; }
}
