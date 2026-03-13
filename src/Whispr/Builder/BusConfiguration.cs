namespace Whispr.Builder;

/// <summary>
/// Configuration for a named bus.
/// </summary>
public sealed class BusConfiguration
{
    /// <summary>
    /// Gets or sets the name of the bus.
    /// </summary>
    public required string BusName { get; init; }

    /// <summary>
    /// Gets or sets the message handler descriptors.
    /// </summary>
    public required List<MessageHandlerDescriptor> MessageHandlerDescriptors { get; init; }
}
