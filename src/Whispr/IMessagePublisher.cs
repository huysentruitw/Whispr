namespace Whispr;

/// <summary>
/// Represents the message publisher.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message.
    /// </summary>
    ValueTask Publish<TMessage>(TMessage message, Action<PublishOptions>? configure = null, CancellationToken cancellationToken = default)
        where TMessage : class;
}

/// <summary>
/// Represents the options for publishing a message.
/// </summary>
public sealed record PublishOptions
{
    internal Dictionary<string, string> Headers { get; } = new();

    internal string? CorrelationId { get; private set; }

    internal DateTimeOffset? DeferredUntil { get; private set; }

    /// <summary>
    /// Sets a header for the message.
    /// </summary>
    /// <param name="key">The header key.</param>
    /// <param name="value">The header value.</param>
    /// <returns>The <see cref="PublishOptions"/> instance.</returns>
    public PublishOptions SetHeader(string key, string value)
    {
        Headers[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the correlation ID for the message.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <returns>The <see cref="PublishOptions"/> instance.</returns>
    public PublishOptions Correlate(string correlationId)
    {
        CorrelationId = correlationId;
        return this;
    }

    /// <summary>
    /// Defer the message until the specified date and time.
    /// </summary>
    /// <param name="deferredUntil">The date and time at which the message should be deferred.</param>
    /// <returns>The <see cref="PublishOptions"/> instance.</returns>
    public PublishOptions Defer(DateTimeOffset? deferredUntil)
    {
        DeferredUntil = deferredUntil;
        return this;
    }
}
