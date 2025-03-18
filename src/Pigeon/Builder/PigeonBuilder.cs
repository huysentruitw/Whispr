namespace Pigeon;

/// <summary>
/// A builder for configuring Pigeon.
/// </summary>
public sealed class PigeonBuilder
{
    internal PigeonBuilder(IServiceCollection services)
    {
        Services = services.AddSingleton<IEnumerable<MessageHandlerDescriptor>>(MessageHandlerDescriptors);
    }

    internal List<MessageHandlerDescriptor> MessageHandlerDescriptors { get; } = [];

    /// <summary>
    /// Gets the original <see cref="IServiceCollection"/> to continue configuring other services.
    /// </summary>
    public IServiceCollection Services { get; }
}

internal sealed record MessageHandlerDescriptor
{
    public required Type HandlerType { get; init; }

    public required Type[] MessageTypes { get; init; }
}
