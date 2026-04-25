using System.Collections.Concurrent;
using System.Reflection;

namespace Whispr.Bus;

/// <inheritdoc />
internal sealed class MessageBusInitializer(
    string busName,
    IEnumerable<MessageHandlerDescriptor> messageHandlerDescriptors,
    IQueueNamingConvention queueNamingConvention,
    ITopicNamingConvention topicNamingConvention,
    ITransport transport,
    IServiceScopeFactory serviceScopeFactory,
    IDiagnosticEventListener diagnosticEventListener,
    ILogger<MessageBusInitializer> logger) : IMessageBusInitializer
{
    private static readonly ConcurrentDictionary<(Type HandlerType, Type MessageType), MethodInfo?> MessageProcessorFactoryCache = new();
    
    public async ValueTask Start(CancellationToken cancellationToken = default)
    {
        using var _ = diagnosticEventListener.Start(busName);

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
                logger.LogInformation("Starting listener for bus: {BusName}, queue: {QueueName} and topics: {TopicNames}", busName, queueName, topicNames);
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
        // Create a scoped message processor and forward the envelope
        var messageType = descriptor.MessageTypes.SingleOrDefault(type => type.FullName == serializedEnvelope.MessageType)
            ?? throw new InvalidOperationException($"Handler: {descriptor.HandlerType} doesn't support message type: {serializedEnvelope.MessageType}");
        
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var processor = CreateMessageProcessor(descriptor.HandlerType, messageType, scope.ServiceProvider);
        await processor.Process(queueName, serializedEnvelope, cancellationToken);
    }

    private IMessageProcessor CreateMessageProcessor(Type handlerType, Type messageType, IServiceProvider serviceProvider)
    {
        // Construct a type-safe factory method for creating message processors, and cache it for future use.
        var factory = MessageProcessorFactoryCache.GetOrAdd(
            (handlerType, messageType),
            static key => typeof(MessageBusInitializer)
                .GetMethod(nameof(CreateMessageProcessor), BindingFlags.NonPublic | BindingFlags.Static)
                ?.MakeGenericMethod(key.HandlerType, key.MessageType));
        
        if (factory is null)
            throw new InvalidOperationException($"Failed to get method: {nameof(CreateMessageProcessor)} for handler type: {handlerType} and message type: {messageType}");

        return (IMessageProcessor)factory.Invoke(null, [serviceProvider, busName])!;
    }
    
    // Type-safe helper method to create a message processor for a specific handler and message type.
    private static IMessageProcessor CreateMessageProcessor<TMessageHandler, TMessage>(IServiceProvider serviceProvider, string busName)
        where TMessageHandler : IMessageHandler<TMessage> where TMessage : class
        => new MessageProcessor<TMessageHandler, TMessage>(
            busName,
            serviceProvider.GetKeyedServices<IConsumeFilter>(busName),
            serviceProvider.GetRequiredKeyedService<TMessageHandler>(busName),
            serviceProvider.GetRequiredService<IDiagnosticEventListener>());
}
