namespace Pigeon;

public static class PigeonServiceCollectionExtensions
{
    public static PigeonBuilder AddPigeon(this IServiceCollection services, Action<PigeonOptions> configureOptions)
    {
        services
            .Configure(configureOptions)
            .AddSingleton<IMessageBus, MessageBus>()
            .AddScoped<MessagePublisher>();

        return new PigeonBuilder(services);
    }
}
