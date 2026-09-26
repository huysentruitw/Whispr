namespace Whispr.AzureServiceBus;

/// <summary>
/// Options for Azure Service Bus.
/// </summary>
public sealed record AzureServiceBusOptions
{
    /// <summary>
    /// The connection string to the Azure Service Bus.
    /// </summary>
    public string? ConnectionString { get; set; } = null!;

    /// <summary>
    /// The host name of the Azure Service Bus to used with managed identity.
    /// </summary>
    public string? HostName { get; set; } = null!;

    /// <summary>
    /// The token credential to use for managed identity authentication.
    /// This is used when <see cref="HostName"/> is set.
    /// When not set, the default Azure credential is used.
    /// </summary>
    public TokenCredential? TokenCredential { get; set; } = null!;

    /// <summary>
    /// The maximum number of concurrent messages allowed to be processed from the same queue.
    /// </summary>
    /// <remarks>This is a per queue setting. Different queues already process messages in parallel.</remarks>
    public int QueueConcurrencyLimit { get; set; } = 1;

    /// <summary>
    /// The delay before a failed message is made available for reprocessing, after its first delivery.
    /// The delay doubles with each delivery. Defaults to <see cref="TimeSpan.Zero"/>, which retries immediately.
    /// </summary>
    /// <remarks>
    /// While waiting, the message stays locked and occupies one of the <see cref="QueueConcurrencyLimit"/> slots.
    /// </remarks>
    public TimeSpan RetryBackoffBase { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// The maximum delay before a failed message is made available for reprocessing.
    /// </summary>
    /// <remarks>Keep this well below the lock renewal duration of 5 minutes, or the message lock is lost while waiting.</remarks>
    public TimeSpan RetryBackoffMax { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The options applied when a queue is created.
    /// </summary>
    public QueueCreationOptions QueueCreation { get; } = new();

    /// <summary>
    /// The options applied when a topic is created.
    /// </summary>
    public TopicCreationOptions TopicCreation { get; } = new();
}
