using Pigeon.AzureServiceBus.Tests.Messages;

namespace Pigeon.AzureServiceBus.Tests.Handlers;

internal sealed class ChirpHandler
    : IMessageHandler<ChirpHeard>
{
    public ValueTask Handle(ChirpHeard message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
