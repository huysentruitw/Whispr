namespace Whispr;

/// <summary>
/// Represents the message bus initializer.
/// </summary>
public interface IMessageBusInitializer
{
    /// <summary>
    /// Starts the message bus.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask Start(CancellationToken cancellationToken = default);
}
