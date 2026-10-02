namespace Whispr;

/// <summary>
/// Thrown when a received message can't be deserialized.
/// </summary>
public sealed class MessageDeserializationException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageDeserializationException"/> class.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <param name="innerException">The exception that caused the deserialization to fail, if any.</param>
    public MessageDeserializationException(string messageType, Exception? innerException = null)
        : base($"Failed to deserialize message envelope of type: {messageType}", innerException)
    {
        MessageType = messageType;
    }

    /// <summary>
    /// The message type.
    /// </summary>
    public string MessageType { get; }
}
