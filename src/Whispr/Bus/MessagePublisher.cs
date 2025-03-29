﻿namespace Whispr.Bus;

/// <inheritdoc />
internal sealed class MessagePublisher(
    IEnumerable<IPublishFilter> publishFilters,
    ITopicNamingConvention topicNamingConvention,
    IMessageSender sender,
    IDiagnosticEventListener diagnosticEventListener,
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
            MessageId = Guid.NewGuid().ToString("N"),
            Message = message,
            MessageType = messageType,
            Headers = options.Headers,
            DestinationTopicName = topicNamingConvention.Format(typeof(TMessage)),
            CorrelationId = options.CorrelationId,
            DeferredUntil = options.DeferredUntil,
        };

        using var publishScope = diagnosticEventListener.Publish(envelope);

        // Build the publishing pipeline
        Func<Envelope<TMessage>, CancellationToken, ValueTask> pipeline = Publish;
        foreach (var publishFilter in publishFilters.Reverse())
        {
            var next = pipeline;
            pipeline = (e, ct) => publishFilter.Publish(e, next, ct);
        }

        // Execute the publishing pipeline
        return pipeline(envelope, cancellationToken);
    }

    private ValueTask Publish<TMessage>(Envelope<TMessage> envelope, CancellationToken cancellationToken)
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

        // When an outbox is available, we add the message to the outbox instead of sending it directly.
        return outbox?.Add(envelope.DestinationTopicName, serializedEnvelope, cancellationToken)
            ?? sender.Send(envelope.DestinationTopicName, serializedEnvelope, cancellationToken);
    }
}
