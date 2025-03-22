namespace Pigeon.Bus;

/// <inheritdoc />
internal sealed class MessagePublisher(
    IEnumerable<IPublishFilter> publishFilters,
    ITopicNamingConvention topicNamingConvention,
    ITransport transport,
    IOutbox? outbox = null) : IMessagePublisher
{
    public ValueTask Publish<TMessage>(TMessage message, Action<PublishOptions>? configure, CancellationToken cancellationToken)
        where TMessage : class
    {
        var messageType = message.GetType().FullName
            ?? throw new InvalidOperationException("Message type must have a full name");

        var options = new PublishOptions();
        configure?.Invoke(options);

        var envelope = new Envelope<TMessage>
        {
            MessageId = Guid.NewGuid().ToString(),
            Message = message,
            MessageType = messageType,
            Headers = options.Headers,
            DestinationTopicName = topicNamingConvention.Format(typeof(TMessage)),
            CorrelationId = options.CorrelationId ?? Guid.NewGuid().ToString(),
            DeferredUntil = options.DeferredUntil,
        };

        // Build the publishing pipeline
        Func<Envelope<TMessage>, CancellationToken, ValueTask> first = Send;
        foreach (var publishFilter in publishFilters.Reverse())
        {
            var next = first;
            first = (e, ct) => publishFilter.Publish(e, next, ct);
        }

        // Execute the publishing pipeline
        return first(envelope, cancellationToken);
    }

    private ValueTask Send<TMessage>(Envelope<TMessage> envelope, CancellationToken cancellationToken)
        where TMessage : class
    {
        var serializedEnvelope = new SerializedEnvelope
        {
            Body = JsonSerializer.Serialize(envelope),
            MessageType = envelope.MessageType,
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            DeferredUntil = envelope.DeferredUntil,
        };

        // When an outbox is available, we add the message to the outbox instead of sending it directly to the transport.
        return outbox?.Add(envelope.DestinationTopicName, serializedEnvelope, cancellationToken)
            ?? transport.Send(envelope.DestinationTopicName, serializedEnvelope, cancellationToken);
    }
}
