using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Whispr;

/// <summary>
/// Service collection extensions.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Whispr to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="busName">The name of the bus.</param>
    /// <returns>The <see cref="WhisprBuilder"/>.</returns>
    public static WhisprBuilder AddWhispr(this IServiceCollection services, string? busName = null)
    {
        busName ??= WhisprDefaults.DefaultBusName;
        
        services.TryAddSingleton<IDiagnosticEventListener, ActivityDiagnosticEventListener>();

        services.AddSingleton<IHostedService>(serviceProvider => new MessageBusLifecycleManager(
            busName,
            serviceProvider.GetRequiredKeyedService<IEnumerable<MessageHandlerDescriptor>>(busName),
            serviceProvider.GetRequiredKeyedService<IQueueNamingConvention>(busName),
            serviceProvider.GetRequiredKeyedService<ITopicNamingConvention>(busName),
            serviceProvider.GetRequiredKeyedService<ITransport>(busName),
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            serviceProvider.GetRequiredService<IDiagnosticEventListener>(),
            serviceProvider.GetRequiredService<ILogger<MessageBusLifecycleManager>>()));

        services.AddKeyedSingleton<IMessageSender>(
            busName,
            (serviceProvider, key) => new MessageSender(
                busName,
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                serviceProvider.GetRequiredKeyedService<ITransport>(key),
                serviceProvider.GetRequiredService<IDiagnosticEventListener>()));
        
        services.AddKeyedScoped<IMessagePublisher>(
            busName,
            (serviceProvider, key) => new MessagePublisher(
                busName,
                serviceProvider.GetKeyedServices<IPublishFilter>(key),
                serviceProvider.GetRequiredKeyedService<ITopicNamingConvention>(key),
                serviceProvider.GetRequiredKeyedService<IMessageSender>(key),
                serviceProvider.GetRequiredService<IDiagnosticEventListener>(),
                serviceProvider.GetKeyedService<IOutbox>(key)));

        // Configure publisher for default bus
        if (busName == WhisprDefaults.DefaultBusName)
        {
            services.AddScoped<IMessagePublisher>(serviceProvider
                => serviceProvider.GetRequiredKeyedService<IMessagePublisher>(WhisprDefaults.DefaultBusName));
        }
        
        return new WhisprBuilder(services, busName);
    }
}
