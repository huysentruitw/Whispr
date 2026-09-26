namespace Whispr.EntityFrameworkCore;

/// <summary>
/// Options for the outbox processor.
/// </summary>
public sealed record OutboxOptions
{
    /// <summary>
    /// The delay between each query to the outbox table when there are more messages to process.
    /// </summary>
    public TimeSpan QueryDelay { get; set; } = TimeSpan.FromMilliseconds(10);

    /// <summary>
    /// The delay between each query to the outbox table when there are no messages to process.
    /// </summary>
    /// <remarks>In normal cases, the outbox will be triggered automatically.</remarks>
    public TimeSpan IdleQueryDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The maximum number of messages to process in a single batch.
    /// </summary>
    public int MaxMessageBatchSize { get; set; } = 100;

    /// <summary>
    /// The maximum number of send attempts, after which a message is parked by setting <see cref="OutboxMessage.ParkedAtUtc"/>.
    /// If <see langword="null"/>, a message is never parked and retried until it's sent.
    /// </summary>
    /// <remarks>Keep in mind that a transport outage causes all send attempts to fail, so a low value can park a lot of messages.</remarks>
    public int? MaxSendAttempts { get; set; }

    /// <summary>
    /// The delay before the first retry of a message. The delay doubles with each failed send attempt.
    /// </summary>
    public TimeSpan RetryBackoffBase { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The maximum delay between retries of a message.
    /// </summary>
    public TimeSpan RetryBackoffMax { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Set to <see langword="true"/> to enable message retention.
    /// </summary>
    public bool EnableMessageRetention { get; set; } = true;

    /// <summary>
    /// Processed message retention period.
    /// </summary>
    public TimeSpan ProcessedMessageRetentionPeriod { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// The delay between each cleanup of the processed messages.
    /// </summary>
    public TimeSpan ProcessedMessageCleanupDelay { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// The maximum number of messages to process in a single batch during cleanup.
    /// </summary>
    public int ProcessedMessageCleanupBatchSize { get; set; } = 100;
}
