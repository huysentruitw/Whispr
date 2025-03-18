namespace Pigeon.Bus;

/// <inheritdoc />
internal sealed class MessagePublisher(
    IEnumerable<IPublishFilter> publishFilters,
    IEnumerable<ISendFilter> sendFilters,
    ITopicNamingConvention topicNamingConvention,
    ITransport transport) : IMessagePublisher
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

        // Build the sending pipeline
        Func<string, SerializedEnvelope, CancellationToken, ValueTask> first = transport.Send;
        foreach (var sendFilter in sendFilters.Reverse())
        {
            var next = first;
            first = (topicName, se, ct) => sendFilter.Send(topicName, se, next, ct);
        }

        // Execute the sending pipeline
        return first(envelope.DestinationTopicName, serializedEnvelope, cancellationToken);
    }
}
