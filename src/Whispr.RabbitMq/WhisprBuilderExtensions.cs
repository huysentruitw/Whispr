using Microsoft.Extensions.Logging;
using Whispr.RabbitMq.Management;
using Whispr.RabbitMq.Transport;

namespace Whispr.RabbitMq;

/// <summary>
/// Extension methods for configuring Whispr with RabbitMQ.
/// </summary>
public static class WhisprBuilderExtensions
{
    /// <summary>
    /// Adds RabbitMQ transport to Whispr.
    /// </summary>
    /// <param name="builder">The <see cref="WhisprBuilder"/>.</param>
    /// <param name="configureOptions">The configuration action for <see cref="RabbitMqOptions"/>.</param>
    /// <returns>The <see cref="WhisprBuilder"/>.</returns>
    public static WhisprBuilder AddRabbitMqTransport(this WhisprBuilder builder, Action<RabbitMqOptions> configureOptions)
    {
        var optionsName = $"RabbitMq_{builder.BusName}";
        builder.Services.Configure(optionsName, configureOptions);

        builder.Services.TryAddKeyedSingleton<ConnectionProvider>(
            builder.BusName,
            (serviceProvider, _) =>
            {
                var options = serviceProvider.GetRequiredService<IOptionsMonitor<RabbitMqOptions>>().Get(optionsName);
                return new ConnectionProvider(options);
            });

        builder.Services.TryAddKeyedSingleton<PublishChannelPool>(
            builder.BusName,
            (serviceProvider, key) => new PublishChannelPool(serviceProvider.GetRequiredKeyedService<ConnectionProvider>(key)));

        builder.Services.TryAddKeyedSingleton<TopologyManager>(
            builder.BusName,
            (serviceProvider, key) => new TopologyManager(serviceProvider.GetRequiredKeyedService<ConnectionProvider>(key)));

        builder.Services.TryAddKeyedSingleton<ITransport>(
            builder.BusName,
            (serviceProvider, key) =>
            {
                var options = serviceProvider.GetRequiredService<IOptionsMonitor<RabbitMqOptions>>().Get(optionsName);

                return new RabbitMqTransport(
                    connectionProvider: serviceProvider.GetRequiredKeyedService<ConnectionProvider>(key),
                    publishChannelPool: serviceProvider.GetRequiredKeyedService<PublishChannelPool>(key),
                    topologyManager: serviceProvider.GetRequiredKeyedService<TopologyManager>(key),
                    options,
                    serviceProvider.GetRequiredService<ILogger<RabbitMqTransport>>());
            });

        return builder;
    }
}
