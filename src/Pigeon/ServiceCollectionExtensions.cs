namespace Pigeon;

public static class ServiceCollectionExtensions
{
    public static PigeonBuilder AddPigeon(this IServiceCollection services)
    {
        services
            .AddSingleton<IMessageBus, MessageBus>()
            .AddScoped<MessagePublisher>();

        return new PigeonBuilder(services);
    }
}
