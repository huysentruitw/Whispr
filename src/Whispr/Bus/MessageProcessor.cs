namespace Whispr.Bus;

internal sealed class MessageProcessor<TMessageHandler, TMessage>(
    IEnumerable<IConsumeFilter> consumeFilters,
    TMessageHandler handler,
    IDiagnosticEventListener diagnosticEventListener) : IMessageProcessor
    where TMessageHandler : IMessageHandler<TMessage>
    where TMessage : class
{
    public ValueTask Process(string queueName, SerializedEnvelope serializedEnvelope, CancellationToken cancellationToken = default)
    {
        using var _ = diagnosticEventListener.Consume(
            consumerName: typeof(TMessageHandler).Name,
            queueName: queueName,
            envelope: serializedEnvelope);

        var envelope = JsonSerializer.Deserialize<Envelope<TMessage>>(serializedEnvelope.Body)
            ?? throw new InvalidOperationException("Failed to deserialize message envelope");

        // Build the consuming pipeline
        Func<Envelope<TMessage>, CancellationToken, ValueTask> pipeline = handler.Handle;
        foreach (var consumeFilter in consumeFilters.Reverse())
        {
            var next = pipeline;
            pipeline = (e, ct) => consumeFilter.Consume(queueName, e, next, ct);
        }

        // Execute the consuming pipeline
        return pipeline(envelope, cancellationToken);
    }
}

internal interface IMessageProcessor
{
    ValueTask Process(string queueName, SerializedEnvelope serializedEnvelope, CancellationToken cancellationToken = default);
}
