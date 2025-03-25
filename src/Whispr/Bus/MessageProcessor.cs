namespace Whispr.Bus;

internal sealed class MessageProcessor<TMessageHandler, TMessage>(
    IEnumerable<IConsumeFilter> consumeFilters,
    TMessageHandler handler) : IMessageProcessor
    where TMessageHandler : IMessageHandler<TMessage>
    where TMessage : class
{
    public ValueTask Process(SerializedEnvelope serializedEnvelope, CancellationToken cancellationToken = default)
    {
        var envelope = JsonSerializer.Deserialize<Envelope<TMessage>>(serializedEnvelope.Body)
            ?? throw new InvalidOperationException("Failed to deserialize message envelope");

        // Build the consuming pipeline
        Func<Envelope<TMessage>, CancellationToken, ValueTask> pipeline = handler.Handle;
        foreach (var publishFilter in consumeFilters.Reverse())
        {
            var next = pipeline;
            pipeline = (e, ct) => publishFilter.Consume(e, next, ct);
        }

        // Execute the consuming pipeline
        return pipeline(envelope, cancellationToken);
    }
}

internal interface IMessageProcessor
{
    ValueTask Process(SerializedEnvelope serializedEnvelope, CancellationToken cancellationToken = default);
}
