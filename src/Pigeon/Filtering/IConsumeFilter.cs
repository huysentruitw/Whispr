namespace Pigeon.Filtering;

public interface IConsumeFilter
{
    ValueTask Consume<TMessage>(Envelope<TMessage> envelope, Func<Envelope<TMessage>, CancellationToken, ValueTask> next, CancellationToken cancellationToken)
        where TMessage : class;
}
