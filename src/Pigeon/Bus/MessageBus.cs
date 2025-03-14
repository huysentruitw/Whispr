namespace Pigeon.Bus;

internal sealed class MessageBus(IServiceProvider serviceProvider) : IMessageBus
{
    public async Task Publish<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<MessagePublisher>();
        await publisher.Publish(message, cancellationToken);
    }
}
