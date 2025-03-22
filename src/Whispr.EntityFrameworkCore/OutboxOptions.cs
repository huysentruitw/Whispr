namespace Whispr.EntityFrameworkCore;

/// <summary>
/// Options for the outbox processor.
/// </summary>
public sealed record OutboxOptions
{
    /// <summary>
    /// The delay between each query to the outbox table.
    /// </summary>
    /// <remarks>In normal cases, the outbox will be triggered automatically.</remarks>
    public TimeSpan QueryDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The maximum number of messages to process in a single batch.
    /// </summary>
    public int MaxMessageBatchSize { get; set; } = 100;

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
