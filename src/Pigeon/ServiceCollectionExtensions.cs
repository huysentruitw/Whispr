namespace Pigeon;

public static class ServiceCollectionExtensions
{
    public static PigeonBuilder AddPigeon(this IServiceCollection services)
    {
        services
            .AddSingleton<IMessageBusInitializer, MessageBusInitializer>()
            .AddScoped<IMessagePublisher, MessagePublisher>()
            .AddScoped(typeof(MessageProcessor<,>));

        return new PigeonBuilder(services);
    }
}
