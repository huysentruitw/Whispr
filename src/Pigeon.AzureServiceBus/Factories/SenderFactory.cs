using System.Collections.Concurrent;

namespace Pigeon.AzureServiceBus.Factories;

internal sealed class SenderFactory(ServiceBusClient client) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();

    public ServiceBusSender GetOrCreateSender(string envelopeDestination)
        => _senders.GetOrAdd(envelopeDestination, client.CreateSender);

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
            await sender.DisposeAsync();
    }
}
