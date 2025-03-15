using System.Collections.Concurrent;

namespace Pigeon.AzureServiceBus.Factories;

internal sealed class ProcessorFactory(ServiceBusClient client) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ServiceBusProcessor> _processors = new();

    public ServiceBusProcessor GetOrCreateProcessor(string queueName)
        => _processors.GetOrAdd(queueName, _ => client.CreateProcessor(queueName, GetProcessorOptions()));

    private static ServiceBusProcessorOptions GetProcessorOptions()
    {
        return new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            PrefetchCount = Environment.ProcessorCount,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            MaxConcurrentCalls = 1,
        };
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _processors.Values)
            await sender.DisposeAsync();
    }
}
