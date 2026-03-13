using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Whispr;

/// <summary>
/// Service collection extensions.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string DefaultBusName = "default";

    /// <summary>
    /// Adds Whispr to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="busName">Optional name for the bus. If not specified, "default" will be used.</param>
    /// <returns>The <see cref="WhisprBuilder"/>.</returns>
    public static WhisprBuilder AddWhispr(this IServiceCollection services, string? busName = null)
    {
        var actualBusName = busName ?? DefaultBusName;

        // Register the bus registry as a singleton (shared across all buses)
        services.TryAddSingleton<IBusRegistry, BusRegistry>();

        // Register core services with keyed DI for multi-bus support
        services.TryAddKeyedSingleton<IMessageBusInitializer>(actualBusName, (sp, key) =>
        {
            var keyStr = key?.ToString() ?? DefaultBusName;
            return new MessageBusInitializer(
                sp.GetRequiredKeyedService<IEnumerable<MessageHandlerDescriptor>>(keyStr),
                sp.GetRequiredKeyedService<IQueueNamingConvention>(keyStr),
                sp.GetRequiredKeyedService<ITopicNamingConvention>(keyStr),
                sp.GetRequiredKeyedService<ITransport>(keyStr),
                sp,
                sp.GetRequiredKeyedService<IDiagnosticEventListener>(keyStr),
                sp.GetRequiredService<ILogger<MessageBusInitializer>>(),
                keyStr);
        });

        services.TryAddKeyedSingleton<IDiagnosticEventListener, ActivityDiagnosticEventListener>(actualBusName);

        services.TryAddKeyedSingleton<IMessageSender>(actualBusName, (sp, key) =>
        {
            var keyStr = key?.ToString() ?? DefaultBusName;
            return new MessageSender(
                sp,
                sp.GetRequiredKeyedService<ITransport>(keyStr),
                sp.GetRequiredKeyedService<IDiagnosticEventListener>(keyStr),
                keyStr);
        });

        services.TryAddKeyedScoped<IMessagePublisher>(actualBusName, (sp, key) =>
        {
            var keyStr = key?.ToString() ?? DefaultBusName;
            return new MessagePublisher(
                sp.GetKeyedServices<IPublishFilter>(keyStr),
                sp.GetRequiredKeyedService<ITopicNamingConvention>(keyStr),
                sp.GetRequiredKeyedService<IMessageSender>(keyStr),
                sp.GetRequiredKeyedService<IDiagnosticEventListener>(keyStr),
                sp.GetService<IOutbox>());
        });

        // Register the message publisher factory
        services.TryAddSingleton<IMessagePublisherFactory, MessagePublisherFactory>();

        // Register the global message bus initializer (starts all buses)
        services.TryAddSingleton<IGlobalMessageBusInitializer, GlobalMessageBusInitializer>();

        return new WhisprBuilder(services, actualBusName);
    }
}
