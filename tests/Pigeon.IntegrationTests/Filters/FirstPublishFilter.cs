using Pigeon.Filtering;

namespace Pigeon.IntegrationTests.Tests.Filters;

public sealed class FirstPublishFilter : IPublishFilter
{
    public ValueTask Publish<TMessage>(
        Envelope<TMessage> message,
        Func<Envelope<TMessage>, CancellationToken, ValueTask> next,
        CancellationToken cancellationToken) where TMessage : class
    {
        return next(message, cancellationToken);
    }
}
