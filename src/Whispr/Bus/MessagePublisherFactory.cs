namespace Whispr.Bus;

/// <inheritdoc />
internal sealed class MessagePublisherFactory(IServiceProvider serviceProvider) : IMessagePublisherFactory
{
    private const string DefaultBusName = "default";

    public IMessagePublisher GetPublisher(string? busName = null)
    {
        var actualBusName = busName ?? DefaultBusName;
        return serviceProvider.GetRequiredKeyedService<IMessagePublisher>(actualBusName);
    }
}
