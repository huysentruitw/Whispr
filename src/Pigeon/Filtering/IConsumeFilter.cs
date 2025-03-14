namespace Pigeon.Filtering;

public interface IConsumeFilter
{
    Task Consume<TMessage>(Envelope<TMessage> envelope, Func<Envelope<TMessage>, CancellationToken, Task> next, CancellationToken cancellationToken)
        where TMessage : class;
}
