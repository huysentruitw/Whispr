namespace Whispr.AzureServiceBus;

/// <summary>
/// Options applied when Whispr creates a queue. Existing queues are not updated.
/// </summary>
public sealed record QueueCreationOptions
{
    /// <summary>
    /// The idle interval after which the queue is automatically deleted.
    /// </summary>
    public TimeSpan AutoDeleteOnIdle { get; set; } = TimeSpan.FromDays(427);

    /// <summary>
    /// The default time to live of messages in the queue.
    /// </summary>
    public TimeSpan DefaultMessageTimeToLive { get; set; } = TimeSpan.FromDays(365);

    /// <summary>
    /// The duration a received message is locked for other receivers.
    /// </summary>
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The maximum number of deliveries, after which a message is dead-lettered.
    /// </summary>
    public int MaxDeliveryCount { get; set; } = 5;
}
