using Azure.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        // Configure options with a unique key per bus
        var optionsName = $"AzureServiceBus_{builder.BusName}";
        builder.Services.Configure<AzureServiceBusOptions>(optionsName, configureOptions);

        // Register the transport as a keyed singleton for this bus
        builder.Services.TryAddKeyedSingleton<ITransport>(builder.BusName, (serviceProvider, key) =>
        {
            var keyStr = key?.ToString() ?? "default";
            var options = serviceProvider.GetRequiredService<IOptionsSnapshot<AzureServiceBusOptions>>().Get(optionsName);

            // Create ServiceBusClient
            ServiceBusClient serviceBusClient;
            ServiceBusAdministrationClient serviceBusAdministrationClient;

            if (!string.IsNullOrEmpty(options.HostName))
            {
                var credential = options.TokenCredential ?? new DefaultAzureCredential();
                serviceBusClient = new ServiceBusClient(options.HostName, credential);
                serviceBusAdministrationClient = new ServiceBusAdministrationClient(options.HostName, credential);
            }
            else if (!string.IsNullOrEmpty(options.ConnectionString))
            {
                serviceBusClient = new ServiceBusClient(options.ConnectionString);
                serviceBusAdministrationClient = new ServiceBusAdministrationClient(options.ConnectionString);
            }
            else
            {
                throw new InvalidOperationException("Either HostName or ConnectionString must be provided.");
            }

            // Create support services
            var entityManager = new EntityManager(serviceBusAdministrationClient);
            var senderFactory = new SenderFactory(serviceBusClient);
            var processorFactory = new ProcessorFactory(serviceBusClient);
            var subscriptionNamingConvention = serviceProvider.GetRequiredKeyedService<ISubscriptionNamingConvention>(keyStr);
            var logger = serviceProvider.GetRequiredService<ILogger<ServiceBusTransport>>();

            // Create and return transport
            return new ServiceBusTransport(
                senderFactory,
                processorFactory,
                entityManager,
                subscriptionNamingConvention,
                Options.Create(options),
                logger);
        });

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
        builder.Services.TryAddKeyedSingleton<ISubscriptionNamingConvention, T>(builder.BusName);
        return builder;
    }
}
