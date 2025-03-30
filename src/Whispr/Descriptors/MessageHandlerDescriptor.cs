namespace Whispr.Descriptors;

internal sealed record MessageHandlerDescriptor
{
    public required Type HandlerType { get; init; }

    public required Type[] MessageTypes { get; init; }
}
