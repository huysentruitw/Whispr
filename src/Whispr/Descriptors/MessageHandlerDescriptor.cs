namespace Whispr.Descriptors;

/// <summary>
/// Describes a message handler, including all message-types it handles,
/// detected by <see cref="WhisprBuilderExtensions.AddMessageHandlersFromAssembly"/>.
///
/// Retrieve <see cref="IEnumerable{MessageHandlerDescriptor}"/> from the DI container
/// to get all detected message handlers. 
/// </summary>
public sealed record MessageHandlerDescriptor
{
    /// <summary>
    /// The type of the handler.
    /// </summary>
    public required Type HandlerType { get; init; }

    /// <summary>
    /// The message types supported by the handler.
    /// </summary>
    public required Type[] MessageTypes { get; init; }
}
