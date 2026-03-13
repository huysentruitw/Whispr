namespace Whispr.Bus;

/// <inheritdoc />
internal sealed class MessageBusInitializer(
    IEnumerable<MessageHandlerDescriptor> messageHandlerDescriptors,
    IQueueNamingConvention queueNamingConvention,
    ITopicNamingConvention topicNamingConvention,
    ITransport transport,
    IServiceProvider serviceProvider,
    IDiagnosticEventListener diagnosticEventListener,
    ILogger<MessageBusInitializer> logger,
    string busName) : IMessageBusInitializer
{
    public async ValueTask Start(CancellationToken cancellationToken = default)
    {
        using var _ = diagnosticEventListener.Start();

        logger.LogInformation("Starting message bus '{BusName}'...", busName);

        await StartListeners(cancellationToken);

        logger.LogInformation("Message bus '{BusName}' started!", busName);
    }

    private async ValueTask StartListeners(CancellationToken cancellationToken = default)
    {
        var tasks = messageHandlerDescriptors
            .Select(descriptor =>
            {
                var queueName = queueNamingConvention.Format(descriptor.HandlerType);
                var topicNames = descriptor.MessageTypes.Select(topicNamingConvention.Format).ToArray();
                logger.LogInformation("Starting listener for bus '{BusName}', queue: {QueueName} and topics: {TopicNames}", busName, queueName, topicNames);
                return transport.StartListener(queueName, topicNames, (se, ct) => MessageCallback(descriptor, queueName, se, ct), cancellationToken).AsTask();
            });

        await Task.WhenAll(tasks);
    }

    private async ValueTask MessageCallback(
        MessageHandlerDescriptor descriptor,
        string queueName,
        SerializedEnvelope serializedEnvelope,
        CancellationToken cancellationToken = default)
    {
        var messageType = descriptor.MessageTypes.SingleOrDefault(type => type.FullName == serializedEnvelope.MessageType)
            ?? throw new InvalidOperationException($"Handler: {descriptor.HandlerType} doesn't support message type: {serializedEnvelope.MessageType}");

        var messageProcessorType = typeof(MessageProcessor<,>).MakeGenericType(descriptor.HandlerType, messageType);

        await using var scope = serviceProvider.CreateAsyncScope();
        
        // Get the handler
        var handler = scope.ServiceProvider.GetRequiredService(descriptor.HandlerType);
        
        // Get keyed services for this bus
        var consumeFilters = scope.ServiceProvider.GetKeyedServices<IConsumeFilter>(busName);
        var diagnosticListener = scope.ServiceProvider.GetRequiredKeyedService<IDiagnosticEventListener>(busName);
        
        // Create the message processor using ActivatorUtilities for better flexibility
        var processor = (IMessageProcessor)ActivatorUtilities.CreateInstance(
            scope.ServiceProvider,
            messageProcessorType,
            consumeFilters,
            handler,
            diagnosticListener);
        
        await processor.Process(queueName, serializedEnvelope, cancellationToken);
    }
}
