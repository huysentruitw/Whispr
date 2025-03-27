using Azure.Identity;
using Whispr.AzureServiceBus.Conventions;
using Whispr.AzureServiceBus.Factories;
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
        builder.Services
            .Configure(configureOptions)
            .AddSingleton<ServiceBusClient>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;

                if (!string.IsNullOrEmpty(options.HostName))
                    return new ServiceBusClient(options.HostName, new DefaultAzureCredential());

                if (!string.IsNullOrEmpty(options.ConnectionString))
                    return new ServiceBusClient(options.ConnectionString);

                throw new InvalidOperationException("Either HostName or ConnectionString must be provided.");
            })
            .AddSingleton<ServiceBusAdministrationClient>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;

                if (!string.IsNullOrEmpty(options.HostName))
                    return new ServiceBusAdministrationClient(options.HostName, new DefaultAzureCredential());

                if (!string.IsNullOrEmpty(options.ConnectionString))
                    return new ServiceBusAdministrationClient(options.ConnectionString);

                throw new InvalidOperationException("Either HostName or ConnectionString must be provided.");
            })
            .AddSingleton<EntityManager>()
            .AddSingleton<SenderFactory>()
            .AddSingleton<ProcessorFactory>()
            .AddSingleton<ITransport, ServiceBusTransport>();

        return builder;
    }

    /// <summary>
    /// Adds a subscription naming convention to Whispr.
    /// </summary>
    /// <param name="builder">The <see cref="WhisprBuilder"/>.</param>
    /// <typeparam name="T">The type of the subscription naming convention.</typeparam>
    /// <returns>The <see cref="WhisprBuilder"/>.</returns>
    public static WhisprBuilder AddSubscriptionNamingConvention<T>(this WhisprBuilder builder) where T : class, ISubscriptionNamingConvention
    {
        builder.Services.AddSingleton<ISubscriptionNamingConvention, T>();
        return builder;
    }
}
