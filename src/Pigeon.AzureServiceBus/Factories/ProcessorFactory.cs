using System.Collections.Concurrent;

namespace Pigeon.AzureServiceBus.Factories;

internal sealed class ProcessorFactory(ServiceBusClient client) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ServiceBusProcessor> _processors = new();

    public ServiceBusProcessor GetOrCreateProcessor(string queueName)
        => _processors.GetOrAdd(queueName, _ => client.CreateProcessor(queueName, new ServiceBusProcessorOptions()));

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _processors.Values)
            await sender.DisposeAsync();
    }
}
