namespace Whispr;

internal sealed class WhisprInitializer(IEnumerable<IMessageBusInitializer> initializers)
    : IWhisprInitializer
{
    public async Task Start(CancellationToken cancellationToken = default)
    {
        foreach (var initializer in initializers)
            await initializer.Start(cancellationToken);
    }
}

/// <summary>
/// The initializer that starts Whispr.
/// </summary>
public interface IWhisprInitializer
{
    /// <summary>
    /// Starts Whispr by initializing all registered message buses.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task Start(CancellationToken cancellationToken = default);
}
