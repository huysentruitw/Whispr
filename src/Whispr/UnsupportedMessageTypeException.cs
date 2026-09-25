namespace Whispr;

/// <summary>
/// Thrown when a handler receives a message type it doesn't handle.
/// </summary>
public sealed class UnsupportedMessageTypeException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedMessageTypeException"/> class.
    /// </summary>
    /// <param name="handlerType">The handler type.</param>
    /// <param name="messageType">The unsupported message type.</param>
    public UnsupportedMessageTypeException(Type handlerType, string messageType)
        : base($"Handler: {handlerType} doesn't support message type: {messageType}")
    {
        HandlerType = handlerType;
        MessageType = messageType;
    }

    /// <summary>
    /// The handler type.
    /// </summary>
    public Type HandlerType { get; }

    /// <summary>
    /// The unsupported message type.
    /// </summary>
    public string MessageType { get; }
}
