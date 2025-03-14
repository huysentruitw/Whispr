namespace Pigeon;

public interface IMessageHandler<in TMessage>
    where TMessage : class
{
    Task Handle(TMessage message, CancellationToken cancellationToken);
}
