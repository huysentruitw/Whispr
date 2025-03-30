using System.Collections.Concurrent;

namespace Whispr.AzureServiceBus.Management;

internal sealed class ProcessorFactory(ServiceBusClient client) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ServiceBusProcessor> _processors = new();

    public ServiceBusProcessor GetOrCreateProcessor(string queueName, int concurrencyLimit = 1)
        => _processors.GetOrAdd(queueName, _ => client.CreateProcessor(queueName, GetProcessorOptions(concurrencyLimit)));

    private static ServiceBusProcessorOptions GetProcessorOptions(int concurrencyLimit)
    {
        return new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            PrefetchCount = Environment.ProcessorCount,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            MaxConcurrentCalls = concurrencyLimit,
        };
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _processors.Values)
            await sender.DisposeAsync();
    }
}
