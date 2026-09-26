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
    /// The options applied when a queue is created.
    /// </summary>
    public QueueCreationOptions QueueCreation { get; } = new();

    /// <summary>
    /// The options applied when a topic is created.
    /// </summary>
    public TopicCreationOptions TopicCreation { get; } = new();
}
