
namespace Whispr.Bus;

/// <inheritdoc />
internal sealed class MessageBusInitializer(
    IEnumerable<MessageHandlerDescriptor> messageHandlerDescriptors,
    IQueueNamingConvention queueNamingConvention,
    ITopicNamingConvention topicNamingConvention,
    ITransport transport,
    IServiceProvider serviceProvider,
    IDiagnosticEventListener diagnosticEventListener,
    ILogger<MessageBusInitializer> logger) : IMessageBusInitializer
{
    public async ValueTask Start(CancellationToken cancellationToken = default)
    {
        using var _ = diagnosticEventListener.Start();

        logger.LogInformation("Starting message bus...");

        await StartListeners(cancellationToken);

        logger.LogInformation("Message bus started!");
    }

    private async ValueTask StartListeners(CancellationToken cancellationToken = default)
    {
        var tasks = messageHandlerDescriptors
            .Select(descriptor =>
            {
                var queueName = queueNamingConvention.Format(descriptor.HandlerType);
                var topicNames = descriptor.MessageTypes.Select(topicNamingConvention.Format).ToArray();
                logger.LogInformation("Starting listener for queue: {QueueName} and topics: {TopicNames}", queueName, topicNames);
                return transport.StartListener(queueName, topicNames, (se, ct) => MessageCallback(descriptor, se, ct), cancellationToken).AsTask();
            });

        await Task.WhenAll(tasks);
    }

    private async ValueTask MessageCallback(
        MessageHandlerDescriptor descriptor,
        SerializedEnvelope serializedEnvelope,
        CancellationToken cancellationToken = default)
    {
        var messageType = descriptor.MessageTypes.SingleOrDefault(type => type.FullName == serializedEnvelope.MessageType)
            ?? throw new InvalidOperationException($"Handler: {descriptor.HandlerType} doesn't support message type: {serializedEnvelope.MessageType}");

        var messageProcessorType = typeof(MessageProcessor<,>).MakeGenericType(descriptor.HandlerType, messageType);

        await using var scope = serviceProvider.CreateAsyncScope();
        var processor = (IMessageProcessor)scope.ServiceProvider.GetRequiredService(messageProcessorType);
        await processor.Process(serializedEnvelope, cancellationToken);
    }
}
