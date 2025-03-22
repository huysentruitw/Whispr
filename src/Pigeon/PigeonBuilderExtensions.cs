using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Pigeon;

/// <summary>
/// Provides extension methods for <see cref="PigeonBuilder"/>.
/// </summary>
public static class PigeonBuilderExtensions
{
    #region MessageHandlers

    /// <summary>
    /// Adds message handlers from the specified assembly.
    /// </summary>
    /// <param name="builder">The <see cref="PigeonBuilder"/>.</param>
    /// <param name="assembly">The assembly to add message handlers from.</param>
    /// <returns>The <see cref="PigeonBuilder"/>.</returns>
    public static PigeonBuilder AddMessageHandlersFromAssembly(this PigeonBuilder builder, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(IsMessageHandler)
            .ToArray();

        foreach (var handlerType in handlerTypes)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
                .ToArray();

            builder.Services.AddScoped(handlerType);

            builder.MessageHandlerDescriptors.Add(new MessageHandlerDescriptor
            {
                HandlerType = handlerType,
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

    /// <summary>
    /// Adds a topic naming convention.
    /// </summary>
    /// <param name="builder">The <see cref="PigeonBuilder"/>.</param>
    /// <typeparam name="TNamingConvention">The type of the topic naming convention.</typeparam>
    /// <returns>The <see cref="PigeonBuilder"/>.</returns>
    public static PigeonBuilder AddTopicNamingConvention<TNamingConvention>(this PigeonBuilder builder)
        where TNamingConvention : class, ITopicNamingConvention
    {
        builder.Services.TryAddSingleton<ITopicNamingConvention, TNamingConvention>();
        return builder;
    }

    /// <summary>
    /// Adds the default topic naming convention.
    /// </summary>
    /// <param name="builder">The <see cref="PigeonBuilder"/>.</param>
    /// <returns>The <see cref="PigeonBuilder"/>.</returns>
    public static PigeonBuilder AddDefaultTopicNamingConvention(this PigeonBuilder builder)
        => builder.AddTopicNamingConvention<DefaultTopicNamingConvention>();

    /// <summary>
    /// Adds a queue naming convention.
    /// </summary>
    /// <param name="builder">The <see cref="PigeonBuilder"/>.</param>
    /// <typeparam name="TNamingConvention">The type of the queue naming convention.</typeparam>
    /// <returns>The <see cref="PigeonBuilder"/>.</returns>
    public static PigeonBuilder AddQueueNamingConvention<TNamingConvention>(this PigeonBuilder builder)
        where TNamingConvention : class, IQueueNamingConvention
    {
        builder.Services.TryAddSingleton<IQueueNamingConvention, TNamingConvention>();
        return builder;
    }

    /// <summary>
    /// Adds the default queue naming convention.
    /// </summary>
    /// <param name="builder">The <see cref="PigeonBuilder"/>.</param>
    /// <returns>The <see cref="PigeonBuilder"/>.</returns>
    public static PigeonBuilder AddDefaultQueueNamingConvention(this PigeonBuilder builder)
        => builder.AddQueueNamingConvention<DefaultQueueNamingConvention>();

    #endregion

    #region Filtering

    /// <summary>
    /// Adds a publish filter.
    /// </summary>
    /// <param name="builder">The <see cref="PigeonBuilder"/>.</param>
    /// <typeparam name="TFilter">The type of the publish filter.</typeparam>
    /// <returns>The <see cref="PigeonBuilder"/>.</returns>
    public static PigeonBuilder AddPublishFilter<TFilter>(this PigeonBuilder builder)
        where TFilter : class, IPublishFilter
    {
        builder.Services.AddScoped<IPublishFilter, TFilter>();
        return builder;
    }

    /// <summary>
    /// Adds a consume filter.
    /// </summary>
    /// <param name="builder">The <see cref="PigeonBuilder"/>.</param>
    /// <typeparam name="TFilter">The type of the consume filter.</typeparam>
    /// <returns>The <see cref="PigeonBuilder"/>.</returns>
    public static PigeonBuilder AddConsumeFilter<TFilter>(this PigeonBuilder builder)
        where TFilter : class, IConsumeFilter
    {
        builder.Services.AddScoped<IConsumeFilter, TFilter>();
        return builder;
    }

    #endregion
}
