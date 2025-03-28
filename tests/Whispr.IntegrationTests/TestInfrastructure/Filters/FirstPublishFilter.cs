using Whispr.Filtering;

namespace Whispr.IntegrationTests.TestInfrastructure.Filters;

public sealed class FirstPublishFilter : IPublishFilter
{
    public ValueTask Publish<TMessage>(
        Envelope<TMessage> envelope,
        Func<Envelope<TMessage>, CancellationToken, ValueTask> next,
        CancellationToken cancellationToken) where TMessage : class
    {
        return next(envelope, cancellationToken);
    }
}
