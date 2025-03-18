namespace Pigeon;

public interface IMessagePublisher
{
    ValueTask Publish<TMessage>(TMessage message, DateTimeOffset? deferredUntil = null, CancellationToken cancellationToken = default)
        where TMessage : class;
}
