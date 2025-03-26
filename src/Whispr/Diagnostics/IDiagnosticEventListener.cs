namespace Whispr.Diagnostics;

/// <summary>
/// Represents a listener for diagnostic events.
/// </summary>
internal interface IDiagnosticEventListener
{
    /// <summary>
    /// Called when the message bus is started.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that can be used to stop listening to the event.</returns>
    IDisposable Start();

    /// <summary>
    /// Called when a message is published.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that can be used to stop listening to the event.</returns>
    IDisposable Publish();

    /// <summary>
    /// Called when a message is sent to the transport.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that can be used to stop listening to the event.</returns>
    IDisposable Send();

    /// <summary>
    /// Called when a message is consumed.
    /// </summary>
    /// <returns>An <see cref="IDisposable"/> that can be used to stop listening to the event.</returns>
    IDisposable Consume();
}
