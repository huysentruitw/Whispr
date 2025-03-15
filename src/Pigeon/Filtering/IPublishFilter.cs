namespace Pigeon.Filtering;

public interface IPublishFilter
{
    ValueTask Publish<TMessage>(Envelope<TMessage> message, Func<Envelope<TMessage>, CancellationToken, ValueTask> next, CancellationToken cancellationToken)
        where TMessage : class;
}
