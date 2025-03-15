namespace Pigeon;

public interface IMessageBus
{
    ValueTask Start(CancellationToken cancellationToken = default);

    ValueTask Publish<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;
}
