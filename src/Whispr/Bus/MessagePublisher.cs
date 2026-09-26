namespace Whispr.Bus;

/// <inheritdoc />
internal sealed class MessagePublisher(
    string busName,
    IEnumerable<IPublishFilter> publishFilters,
    ITopicNamingConvention topicNamingConvention,
    IMessageSender sender,
    IDiagnosticEventListener diagnosticEventListener,
    JsonSerializerOptions jsonSerializerOptions,
    IOutbox? outbox = null) : IMessagePublisher
{
    public async ValueTask Publish<TMessage>(TMessage message, Action<PublishOptions>? configure, CancellationToken cancellationToken)
        where TMessage : class
    {
        // Use the runtime type, so a message published through a base type or interface
        // is routed, typed and serialized as the concrete message type.
        var runtimeType = message.GetType();
        var messageType = runtimeType.FullName
            ?? throw new InvalidOperationException("Message type must have a full name");

        var options = new PublishOptions();
        configure?.Invoke(options);

        var envelope = new Envelope<TMessage>
        {
            MessageId = Guid.NewGuid().ToString("N"),
            Message = message,
            MessageType = messageType,
            PublishedAtUtc = DateTime.UtcNow,
            Headers = options.Headers,
            DestinationTopicName = topicNamingConvention.Format(runtimeType),
            CorrelationId = options.CorrelationId,
            DeferredUntil = options.DeferredUntil,
        };

        using var publishScope = diagnosticEventListener.Publish(busName, envelope);

        // Build the publishing pipeline
        Func<Envelope<TMessage>, CancellationToken, ValueTask> pipeline = Publish;
        foreach (var publishFilter in publishFilters.Reverse())
        {
            var next = pipeline;
            pipeline = (e, ct) => publishFilter.Publish(e, next, ct);
        }

        // Execute the publishing pipeline
        await pipeline(envelope, cancellationToken);
    }

    private ValueTask Publish<TMessage>(Envelope<TMessage> envelope, CancellationToken cancellationToken)
        where TMessage : class
    {
        var serializedEnvelope = new SerializedEnvelope
        {
            Body = Serialize(envelope),
            MessageType = envelope.MessageType,
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            DeferredUntil = envelope.DeferredUntil,
        };

        // When an outbox is available, we add the message to the outbox instead of sending it directly.
        return outbox?.Add(envelope.DestinationTopicName, serializedEnvelope, cancellationToken)
            ?? sender.Send(envelope.DestinationTopicName, serializedEnvelope, cancellationToken);
    }

    private string Serialize<TMessage>(Envelope<TMessage> envelope)
        where TMessage : class
    {
        var runtimeType = envelope.Message.GetType();
        if (runtimeType == typeof(TMessage))
            return JsonSerializer.Serialize(envelope, jsonSerializerOptions);
        
        // System.Text.Json serializes by declared type, which would drop the properties of the concrete message
        var messagePropertyName = GetPropertyName(nameof(Envelope<>.Message));
        var node = JsonSerializer.SerializeToNode(envelope, jsonSerializerOptions)!;
        node[messagePropertyName] = JsonSerializer.SerializeToNode(envelope.Message, runtimeType, jsonSerializerOptions);
        return node.ToJsonString(jsonSerializerOptions);
    }

    private string GetPropertyName(string name)
        => jsonSerializerOptions.PropertyNamingPolicy?.ConvertName(name) ?? name;
}
