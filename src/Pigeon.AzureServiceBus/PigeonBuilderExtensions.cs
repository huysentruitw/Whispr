using Pigeon.AzureServiceBus.Conventions;
using Pigeon.AzureServiceBus.Factories;
using Pigeon.Builder;

namespace Pigeon.AzureServiceBus;

/// <summary>
/// Extension methods for configuring Pigeon with Azure Service Bus.
/// </summary>
public static class PigeonBuilderExtensions
{
    /// <summary>
    /// Adds Azure Service Bus transport to Pigeon.
    /// </summary>
    /// <param name="builder">The <see cref="PigeonBuilder"/>.</param>
    /// <param name="configureOptions">The configuration action for <see cref="AzureServiceBusOptions"/>.</param>
    /// <returns>The <see cref="PigeonBuilder"/>.</returns>
    public static PigeonBuilder AddAzureServiceBusTransport(this PigeonBuilder builder, Action<AzureServiceBusOptions> configureOptions)
    {
        builder.Services
            .Configure(configureOptions)
            .AddSingleton<ServiceBusClient>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;
                return new ServiceBusClient(options.ConnectionString);
            })
            .AddSingleton<ServiceBusAdministrationClient>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;
                return new ServiceBusAdministrationClient(options.ConnectionString);
            })
            .AddSingleton<EntityManager>()
            .AddSingleton<SenderFactory>()
            .AddSingleton<ProcessorFactory>()
            .AddSingleton<ITransport, Transport>();

        return builder;
    }

    /// <summary>
    /// Adds a subscription naming convention to Pigeon.
    /// </summary>
    /// <param name="builder">The <see cref="PigeonBuilder"/>.</param>
    /// <typeparam name="T">The type of the subscription naming convention.</typeparam>
    /// <returns>The <see cref="PigeonBuilder"/>.</returns>
    public static PigeonBuilder AddSubscriptionNamingConvention<T>(this PigeonBuilder builder) where T : class, ISubscriptionNamingConvention
    {
        builder.Services.AddSingleton<ISubscriptionNamingConvention, T>();
        return builder;
    }
}
