namespace Pigeon.Bus;

/// <inheritdoc />
internal sealed class MessageBusInitializer(
    IEnumerable<MessageHandlerDescriptor> messageHandlerDescriptors,
    IQueueNamingConvention queueNamingConvention,
    ITopicNamingConvention topicNamingConvention,
    ITransport transport,
    IServiceProvider serviceProvider) : IMessageBusInitializer
{
    public ValueTask Start(CancellationToken cancellationToken = default)
        => StartListeners(cancellationToken);

    private async ValueTask StartListeners(CancellationToken cancellationToken = default)
    {
        var tasks = messageHandlerDescriptors
            .Select(descriptor =>
            {
                var queueName = queueNamingConvention.Format(descriptor.HandlerType);
                var topicNames = descriptor.MessageTypes.Select(topicNamingConvention.Format).ToArray();
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
