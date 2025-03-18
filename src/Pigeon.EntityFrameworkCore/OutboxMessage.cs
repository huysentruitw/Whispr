namespace Pigeon.EntityFrameworkCore;

/// <summary>
/// Represents an outbox message.
/// </summary>
public sealed record OutboxMessage
{
    /// <summary>
    /// The outbox message ID.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// The serialized envelope.
    /// </summary>
    public required SerializedEnvelope Envelope { get; init; }

    /// <summary>
    /// The destination topic name.
    /// </summary>
    public required string DestinationTopicName { get; init; }

    /// <summary>
    /// The date and time the outbox message was created in UTC.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// The date and time the outbox message was processed in UTC.
    /// </summary>
    public DateTime? ProcessedAtUtc { get; init; }
}
