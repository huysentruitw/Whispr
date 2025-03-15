namespace Pigeon;

public interface IMessageHandler<in TMessage>
    where TMessage : class
{
    ValueTask Handle(TMessage message, CancellationToken cancellationToken);
}
