namespace Whispr;

/// <summary>
/// Initializes all registered message buses.
/// </summary>
public interface IGlobalMessageBusInitializer
{
    /// <summary>
    /// Starts all registered message buses.
    /// </summary>
    ValueTask Start(CancellationToken cancellationToken = default);
}
