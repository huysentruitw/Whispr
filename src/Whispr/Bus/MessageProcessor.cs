namespace Whispr.Bus;

internal sealed class MessageProcessor<TMessageHandler, TMessage>(
    string busName,
    IEnumerable<IConsumeFilter> consumeFilters,
    TMessageHandler handler,
    IDiagnosticEventListener diagnosticEventListener,
    JsonSerializerOptions jsonSerializerOptions) : IMessageProcessor
    where TMessageHandler : IMessageHandler<TMessage>
    where TMessage : class
{
    public async ValueTask Process(string queueName, SerializedEnvelope serializedEnvelope, CancellationToken cancellationToken = default)
    {
        using var _ = diagnosticEventListener.Consume(
            busName: busName,
            consumerName: typeof(TMessageHandler).Name,
            queueName: queueName,
            envelope: serializedEnvelope);

        var envelope = Deserialize(serializedEnvelope);

        // Build the consuming pipeline
        Func<Envelope<TMessage>, CancellationToken, ValueTask> pipeline = handler.Handle;
        foreach (var consumeFilter in consumeFilters.Reverse())
        {
            var next = pipeline;
            pipeline = (e, ct) => consumeFilter.Consume(queueName, e, next, ct);
        }

        // Execute the consuming pipeline
        await pipeline(envelope, cancellationToken);
    }

    private Envelope<TMessage> Deserialize(SerializedEnvelope serializedEnvelope)
    {
        try
        {
            return JsonSerializer.Deserialize<Envelope<TMessage>>(serializedEnvelope.Body, jsonSerializerOptions)
                ?? throw new MessageDeserializationException(serializedEnvelope.MessageType);
        }
        catch (JsonException ex)
        {
            throw new MessageDeserializationException(serializedEnvelope.MessageType, ex);
        }
    }
}

internal interface IMessageProcessor
{
    ValueTask Process(string queueName, SerializedEnvelope serializedEnvelope, CancellationToken cancellationToken = default);
}
