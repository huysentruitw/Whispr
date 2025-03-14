using System.Reflection;

namespace Pigeon;

public static class PigeonBuilderExtensions
{
    public static PigeonBuilder AddMessageHandlersFromAssembly(this PigeonBuilder builder, Assembly assembly)
    {
        var handlers = assembly.GetTypes()
            .Where(IsMessageHandler)
            .ToArray();

        foreach (var handler in handlers)
        {
            var interfaces = handler.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandler<>));
            foreach (var @interface in interfaces)
                builder.Services.AddScoped(@interface, handler);
        }

        return builder;
    }

    private static bool IsMessageHandler(Type type)
        => type is { IsAbstract: false, IsInterface: false } &&
           type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandler<>));

    public static PigeonBuilder AddPublishFilter<TFilter>(this PigeonBuilder builder)
        where TFilter : class, IPublishFilter
    {
        builder.Services.AddScoped<IPublishFilter, TFilter>();
        return builder;
    }

    public static PigeonBuilder AddSendFilter<TFilter>(this PigeonBuilder builder)
        where TFilter : class, ISendFilter
    {
        builder.Services.AddScoped<ISendFilter, TFilter>();
        return builder;
    }
}
