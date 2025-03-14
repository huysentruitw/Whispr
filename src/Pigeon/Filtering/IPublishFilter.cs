namespace Pigeon.Filtering;

public interface IPublishFilter
{
    Task Publish<TMessage>(Envelope<TMessage> message, Func<Envelope<TMessage>, CancellationToken, Task> next, CancellationToken cancellationToken)
        where TMessage : class;
}
