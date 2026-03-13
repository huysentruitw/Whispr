using Azure.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        // Register ServiceBusClient as keyed singleton for this bus
        builder.Services.TryAddKeyedSingleton<ServiceBusClient>(builder.BusName, (serviceProvider, key) =>
        {
            var options = serviceProvider.GetRequiredService<IOptionsSnapshot<AzureServiceBusOptions>>().Get(optionsName);

            if (!string.IsNullOrEmpty(options.HostName))
                return new ServiceBusClient(options.HostName, options.TokenCredential ?? new DefaultAzureCredential());

            if (!string.IsNullOrEmpty(options.ConnectionString))
                return new ServiceBusClient(options.ConnectionString);

            throw new InvalidOperationException("Either HostName or ConnectionString must be provided.");
        });

        // Register ServiceBusAdministrationClient as keyed singleton for this bus
        builder.Services.TryAddKeyedSingleton<ServiceBusAdministrationClient>(builder.BusName, (serviceProvider, key) =>
        {
            var options = serviceProvider.GetRequiredService<IOptionsSnapshot<AzureServiceBusOptions>>().Get(optionsName);

            if (!string.IsNullOrEmpty(options.HostName))
                return new ServiceBusAdministrationClient(options.HostName, options.TokenCredential ?? new DefaultAzureCredential());

            if (!string.IsNullOrEmpty(options.ConnectionString))
                return new ServiceBusAdministrationClient(options.ConnectionString);

            throw new InvalidOperationException("Either HostName or ConnectionString must be provided.");
        });

        // Register per-bus services with keyed DI
        builder.Services.TryAddKeyedSingleton<EntityManager>(builder.BusName, (sp, key) =>
        {
            var keyStr = key?.ToString() ?? "default";
            return new EntityManager(
                sp.GetRequiredKeyedService<ServiceBusAdministrationClient>(keyStr),
                sp.GetRequiredKeyedService<IQueueNamingConvention>(keyStr),
                sp.GetRequiredKeyedService<ITopicNamingConvention>(keyStr),
                sp.GetRequiredKeyedService<ISubscriptionNamingConvention>(keyStr),
                sp.GetRequiredService<ILogger<EntityManager>>());
        });

        builder.Services.TryAddKeyedSingleton<SenderFactory>(builder.BusName, (sp, key) =>
        {
            var keyStr = key?.ToString() ?? "default";
            return new SenderFactory(
                sp.GetRequiredKeyedService<ServiceBusClient>(keyStr));
        });

        builder.Services.TryAddKeyedSingleton<ProcessorFactory>(builder.BusName, (sp, key) =>
        {
            var keyStr = key?.ToString() ?? "default";
            var options = sp.GetRequiredService<IOptionsSnapshot<AzureServiceBusOptions>>().Get(optionsName);
            return new ProcessorFactory(
                sp.GetRequiredKeyedService<ServiceBusClient>(keyStr),
                options);
        });

        builder.Services.TryAddKeyedSingleton<ITransport, ServiceBusTransport>(builder.BusName, (sp, key) =>
        {
            var keyStr = key?.ToString() ?? "default";
            return new ServiceBusTransport(
                sp.GetRequiredKeyedService<EntityManager>(keyStr),
                sp.GetRequiredKeyedService<SenderFactory>(keyStr),
                sp.GetRequiredKeyedService<ProcessorFactory>(keyStr),
                sp.GetRequiredService<ILogger<ServiceBusTransport>>());
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
