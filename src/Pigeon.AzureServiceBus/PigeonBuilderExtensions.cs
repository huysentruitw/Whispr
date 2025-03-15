using Pigeon.AzureServiceBus.Conventions;
using Pigeon.AzureServiceBus.Factories;

namespace Pigeon.AzureServiceBus;

public static class PigeonBuilderExtensions
{
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

    public static PigeonBuilder AddSubscriptionNamingConvention<T>(this PigeonBuilder builder) where T : class, ISubscriptionNamingConvention
    {
        builder.Services.AddSingleton<ISubscriptionNamingConvention, T>();
        return builder;
    }
}
