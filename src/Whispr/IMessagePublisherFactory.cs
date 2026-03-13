namespace Whispr;

/// <summary>
/// Factory for creating message publishers for specific buses.
/// </summary>
public interface IMessagePublisherFactory
{
    /// <summary>
    /// Gets a message publisher for the specified bus.
    /// </summary>
    /// <param name="busName">The name of the bus. If not specified, the default bus will be used.</param>
    /// <returns>The message publisher for the specified bus.</returns>
    IMessagePublisher GetPublisher(string? busName = null);
}
