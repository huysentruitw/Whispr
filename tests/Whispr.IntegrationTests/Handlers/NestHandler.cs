namespace Whispr.IntegrationTests.Tests.Handlers;

internal sealed class NestHandler
    : IMessageHandler<NestBuilt>
    , IMessageHandler<NestAbandoned>
{
    public ValueTask Handle(Envelope<NestBuilt> envelope, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask Handle(Envelope<NestAbandoned> envelope, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
