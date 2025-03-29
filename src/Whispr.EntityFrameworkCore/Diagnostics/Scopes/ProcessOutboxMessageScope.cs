namespace Whispr.EntityFrameworkCore.Diagnostics.Scopes;

internal sealed class ProcessOutboxMessageScope(Activity activity) : IDisposable
{
    private bool _disposed;

    public const string ActivityName = "Whispr.ProcessOutboxMessage";

    public void Dispose()
    {
        if (_disposed)
            return;

        activity.Dispose();
        _disposed = true;
    }
}
