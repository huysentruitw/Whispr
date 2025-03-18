namespace Pigeon;

/// <summary>
/// Service collection extensions.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Pigeon to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The <see cref="PigeonBuilder"/>.</returns>
    public static PigeonBuilder AddPigeon(this IServiceCollection services)
    {
        services
            .AddSingleton<IMessageBusInitializer, MessageBusInitializer>()
            .AddScoped<IMessagePublisher, MessagePublisher>()
            .AddScoped(typeof(MessageProcessor<,>));

        return new PigeonBuilder(services);
    }
}
