namespace Pigeon.Bus;

internal sealed class MessageBus(
    IEnumerable<MessageHandlerDescriptor> messageHandlerDescriptors,
    IQueueNamingConvention queueNamingConvention,
    ITopicNamingConvention topicNamingConvention,
    ITransport transport,
    IServiceProvider serviceProvider) : IMessageBus
{
    public async ValueTask Start(CancellationToken cancellationToken = default)
    {
        var tasks = messageHandlerDescriptors
            .Select(descriptor =>
            {
                var queueName = queueNamingConvention.Format(descriptor.HandlerType);
                var topicNames = descriptor.MessageTypes.Select(topicNamingConvention.Format).ToArray();
                return transport.StartListener(queueName, topicNames, cancellationToken).AsTask();
            });

        await Task.WhenAll(tasks);
    }

    public async ValueTask Publish<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<MessagePublisher>();
        await publisher.Publish(message, cancellationToken);
    }
}
