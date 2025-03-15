namespace Pigeon;

public interface IMessageHandler<TMessage>
    where TMessage : class
{
    ValueTask Handle(Envelope<TMessage> envelope, CancellationToken cancellationToken);
}
