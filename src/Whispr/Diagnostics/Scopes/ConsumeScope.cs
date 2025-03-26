using System.Diagnostics;

namespace Whispr.Diagnostics.Scopes;

internal sealed class ConsumeScope : IDisposable
{
    private readonly Activity _activity;
    private bool _disposed;

    public ConsumeScope(Activity activity)
    {
        _activity = activity;
        _activity.DisplayName = "Whispr.Consume";
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _activity.Dispose();
        _disposed = true;
    }
}
