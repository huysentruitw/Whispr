namespace Whispr.RabbitMq;

/// <summary>
/// Options for RabbitMQ.
/// </summary>
public sealed record RabbitMqOptions
{
    /// <summary>
    /// The AMQP URI of the RabbitMQ broker, e.g. <c>amqp://guest:guest@localhost:5672/</c>.
    /// </summary>
    public string? ConnectionString { get; set; } = null!;

    /// <summary>
    /// The connection name shown in the RabbitMQ management UI. When not set, the client library default is used.
    /// </summary>
    public string? ClientProvidedName { get; set; } = null!;

    /// <summary>
    /// The maximum number of concurrent messages allowed to be processed from the same queue.
    /// </summary>
    /// <remarks>This is a per queue setting. Different queues already process messages in parallel.</remarks>
    public int QueueConcurrencyLimit { get; set; } = 1;

    /// <summary>
    /// The maximum number of deliveries, after which a message is dead-lettered.
    /// </summary>
    public int MaxDeliveryCount { get; set; } = 5;

    /// <summary>
    /// The delay before a failed message is made available for reprocessing, after its first delivery.
    /// The delay doubles with each delivery. Defaults to <see cref="TimeSpan.Zero"/>, which retries immediately.
    /// </summary>
    /// <remarks>
    /// While waiting, the message stays unacknowledged and occupies one of the <see cref="QueueConcurrencyLimit"/> slots.
    /// </remarks>
    public TimeSpan RetryBackoffBase { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// The maximum delay before a failed message is made available for reprocessing.
    /// </summary>
    /// <remarks>Keep this well below the consumer timeout of the broker (30 minutes by default).</remarks>
    public TimeSpan RetryBackoffMax { get; set; } = TimeSpan.FromSeconds(30);
}
