using Azure.Identity;
using Microsoft.Extensions.Logging;
using Whispr.AzureServiceBus.Conventions;
using Whispr.AzureServiceBus.Management;
using Whispr.AzureServiceBus.Transport;

namespace Whispr.AzureServiceBus;

/// <summary>
/// Extension methods for configuring Whispr with Azure Service Bus.
/// </summary>
public static class WhisprBuilderExtensions
{
    /// <summary>
    /// Adds Azure Service Bus transport to Whispr.
    /// </summary>
    /// <param name="builder">The <see cref="WhisprBuilder"/>.</param>
    /// <param name="configureOptions">The configuration action for <see cref="AzureServiceBusOptions"/>.</param>
    /// <returns>The <see cref="WhisprBuilder"/>.</returns>
    public static WhisprBuilder AddAzureServiceBusTransport(this WhisprBuilder builder, Action<AzureServiceBusOptions> configureOptions)
    {
        var optionsName = $"AzureServiceBus_{builder.BusName}";
        builder.Services.Configure(optionsName, configureOptions);

        builder.Services.TryAddKeyedSingleton<ITransport>(
            builder.BusName,
            (serviceProvider, _) =>
            {
                var options = serviceProvider.GetRequiredService<IOptionsMonitor<AzureServiceBusOptions>>().Get(optionsName);

                var serviceBusClient = CreateServiceBusClient(options);
                var serviceBusAdministrationClient = CreateServiceBusAdministrationClient(options);

                return new ServiceBusTransport(
                    new SenderFactory(serviceBusClient),
                    new ProcessorFactory(serviceBusClient),
                    new EntityManager(serviceBusAdministrationClient),
                    serviceProvider.GetRequiredKeyedService<ISubscriptionNamingConvention>(builder.BusName),
                    options,
                    serviceProvider.GetRequiredService<ILogger<ServiceBusTransport>>());
            });
        
        return builder;
    }
    
    private static ServiceBusClient CreateServiceBusClient(AzureServiceBusOptions options)
    {
        if (!string.IsNullOrEmpty(options.HostName))
            return new ServiceBusClient(options.HostName, options.TokenCredential ?? new DefaultAzureCredential());

        if (!string.IsNullOrEmpty(options.ConnectionString))
            return new ServiceBusClient(options.ConnectionString);

        throw new InvalidOperationException("Either HostName or ConnectionString must be provided.");
    }
    
    private static ServiceBusAdministrationClient CreateServiceBusAdministrationClient(AzureServiceBusOptions options)
    {
        if (!string.IsNullOrEmpty(options.HostName))
            return new ServiceBusAdministrationClient(options.HostName, options.TokenCredential ?? new DefaultAzureCredential());

        if (!string.IsNullOrEmpty(options.ConnectionString))
            return new ServiceBusAdministrationClient(options.ConnectionString);

        throw new InvalidOperationException("Either HostName or ConnectionString must be provided.");
    }

    /// <summary>
    /// Adds a subscription naming convention to Whispr.
    /// </summary>
    /// <param name="builder">The <see cref="WhisprBuilder"/>.</param>
    /// <typeparam name="T">The type of the subscription naming convention.</typeparam>
    /// <returns>The <see cref="WhisprBuilder"/>.</returns>
    public static WhisprBuilder AddSubscriptionNamingConvention<T>(this WhisprBuilder builder) where T : class, ISubscriptionNamingConvention
    {
        builder.Services.TryAddKeyedSingleton<ISubscriptionNamingConvention, T>(builder.BusName);
        return builder;
    }

    /// <summary>
    /// Adds a subscription naming convention using a factory method.
    /// </summary>
    /// <param name="builder">The <see cref="WhisprBuilder"/>.</param>
    /// <param name="factory">A factory function that creates the naming convention instance. The function receives the service provider and bus name.</param>
    /// <typeparam name="T">The type of the subscription naming convention.</typeparam>
    /// <returns>The <see cref="WhisprBuilder"/>.</returns>
    public static WhisprBuilder AddSubscriptionNamingConvention<T>(
        this WhisprBuilder builder,
        Func<IServiceProvider, string, T> factory)
        where T : class, ISubscriptionNamingConvention
    {
        builder.Services.TryAddKeyedSingleton<ISubscriptionNamingConvention>(
            builder.BusName,
            (sp, key) => factory(sp, (string)key!));
        return builder;
    }
}
