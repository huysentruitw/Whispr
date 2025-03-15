using System.Reflection;

namespace Pigeon;

public static class PigeonBuilderExtensions
{
    #region MessageHandlers

    public static PigeonBuilder AddMessageHandlersFromAssembly(this PigeonBuilder builder, Assembly assembly)
    {
        var handlers = assembly.GetTypes()
            .Where(IsMessageHandler)
            .ToArray();

        foreach (var handler in handlers)
        {
            var interfaces = handler.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
                .ToArray();

            foreach (var @interface in interfaces)
                builder.Services.AddScoped(@interface, handler);

            builder.MessageHandlerDescriptors.Add(new MessageHandlerDescriptor
            {
                HandlerType = handler,
                MessageTypes = interfaces.Select(i => i.GetGenericArguments().First()).ToArray(),
            });
        }

        return builder;
    }

    private static bool IsMessageHandler(Type type)
        => type is { IsAbstract: false, IsInterface: false } &&
           type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandler<>));

    #endregion

    #region Conventions

    public static PigeonBuilder AddTopicNamingConvention<TNamingConvention>(this PigeonBuilder builder)
        where TNamingConvention : class, ITopicNamingConvention
    {
        builder.Services.AddSingleton<ITopicNamingConvention, TNamingConvention>();
        return builder;
    }

    public static PigeonBuilder AddDefaultTopicNamingConvention(this PigeonBuilder builder)
        => builder.AddTopicNamingConvention<DefaultTopicNamingConvention>();

    public static PigeonBuilder AddQueueNamingConvention<TNamingConvention>(this PigeonBuilder builder)
        where TNamingConvention : class, IQueueNamingConvention
    {
        builder.Services.AddSingleton<IQueueNamingConvention, TNamingConvention>();
        return builder;
    }

    public static PigeonBuilder AddDefaultQueueNamingConvention(this PigeonBuilder builder)
        => builder.AddQueueNamingConvention<DefaultQueueNamingConvention>();

    #endregion

    #region Filtering

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

    #endregion
}
