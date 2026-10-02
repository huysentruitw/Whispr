using System.Collections.Concurrent;

namespace Whispr.AzureServiceBus.Management;

internal sealed class ProcessorFactory(ServiceBusClient client) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ServiceBusProcessor> _processors = new();

    public ServiceBusProcessor GetOrCreateProcessor(string queueName, int concurrencyLimit = 1, int prefetchCount = 1)
        => _processors.GetOrAdd(queueName, _ => client.CreateProcessor(queueName, GetProcessorOptions(concurrencyLimit, prefetchCount)));

    private static ServiceBusProcessorOptions GetProcessorOptions(int concurrencyLimit, int prefetchCount)
    {
        return new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            PrefetchCount = prefetchCount,
            MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5),
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            MaxConcurrentCalls = concurrencyLimit,
        };
    }

    public async ValueTask StopAllProcessors(CancellationToken cancellationToken = default)
    {
        foreach (var processor in _processors.Values)
            await processor.StopProcessingAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var processor in _processors.Values)
            await processor.DisposeAsync();
    }
}
