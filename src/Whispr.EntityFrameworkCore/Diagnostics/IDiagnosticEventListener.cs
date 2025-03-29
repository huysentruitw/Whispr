namespace Whispr.EntityFrameworkCore.Diagnostics;

/// <summary>
/// Represents a listener for diagnostic events.
/// </summary>
internal interface IDiagnosticEventListener
{
    /// <summary>
    /// Called when a message is processed from the outbox.
    /// </summary>
    /// <param name="traceParent"></param>
    /// <returns>An <see cref="IDisposable"/> that can be used to stop listening to the event.</returns>
    IDisposable ProcessOutboxMessage(string? traceParent);
}
