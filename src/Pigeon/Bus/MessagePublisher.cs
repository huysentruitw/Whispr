namespace Pigeon.Bus;

internal sealed class MessagePublisher(
    IEnumerable<IPublishFilter> publishFilters,
    IEnumerable<ISendFilter> sendFilters,
    ITopicNamingConvention topicNamingConvention,
    ITransport transport)
{
    public ValueTask Publish<TMessage>(TMessage message, DateTimeOffset? deferredUntil, CancellationToken cancellationToken)
        where TMessage : class
    {
        var messageType = message.GetType().FullName
            ?? throw new InvalidOperationException("Message type must have a full name");

        var envelope = new Envelope<TMessage>
        {
            Message = message,
            MessageType = messageType,
            TopicName = topicNamingConvention.Format(typeof(TMessage)),
            CorrelationId = Guid.NewGuid().ToString(),
            DeferredUntil = deferredUntil,
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
            TopicName = envelope.TopicName,
            CorrelationId = envelope.CorrelationId,
            DeferredUntil = envelope.DeferredUntil,
        };

        // Build the sending pipeline
        Func<SerializedEnvelope, CancellationToken, ValueTask> first = transport.Send;
        foreach (var sendFilter in sendFilters.Reverse())
        {
            var next = first;
            first = (e, ct) => sendFilter.Send(e, next, ct);
        }

        // Execute the sending pipeline
        return first(serializedEnvelope, cancellationToken);
    }
}
