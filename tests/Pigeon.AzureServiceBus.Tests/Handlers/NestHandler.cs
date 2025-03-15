using Pigeon.AzureServiceBus.Tests.Messages;

namespace Pigeon.AzureServiceBus.Tests.Handlers;

internal sealed class NestHandler
    : IMessageHandler<NestBuilt>
    , IMessageHandler<NestAbandoned>
{
    public ValueTask Handle(NestBuilt message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public ValueTask Handle(NestAbandoned message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
