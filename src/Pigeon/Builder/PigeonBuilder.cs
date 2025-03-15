namespace Pigeon;

public sealed class PigeonBuilder
{
    public PigeonBuilder(IServiceCollection services)
    {
        Services = services.AddSingleton<IEnumerable<MessageHandlerDescriptor>>(MessageHandlerDescriptors);
    }

    internal List<MessageHandlerDescriptor> MessageHandlerDescriptors { get; } = [];

    public IServiceCollection Services { get; }
}

internal sealed record MessageHandlerDescriptor
{
    public required Type HandlerType { get; init; }

    public required Type[] MessageTypes { get; init; }
}
