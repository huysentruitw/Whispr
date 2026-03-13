namespace Whispr.Bus;

/// <inheritdoc />
internal sealed class GlobalMessageBusInitializer(
    IBusRegistry busRegistry,
    IServiceProvider serviceProvider,
    ILogger<GlobalMessageBusInitializer> logger) : IGlobalMessageBusInitializer
{
    public async ValueTask Start(CancellationToken cancellationToken = default)
    {
        var busNames = busRegistry.GetBusNames().ToArray();

        if (busNames.Length == 0)
        {
            logger.LogWarning("No message buses registered. Please use AddWhispr() to register at least one bus.");
            return;
        }

        logger.LogInformation("Starting {BusCount} message bus(es)...", busNames.Length);

        var tasks = busNames.Select(async busName =>
        {
            try
            {
                var initializer = serviceProvider.GetRequiredKeyedService<IMessageBusInitializer>(busName);
                await initializer.Start(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start message bus '{BusName}'", busName);
                throw;
            }
        });

        await Task.WhenAll(tasks);

        logger.LogInformation("All message buses started successfully!");
    }
}
