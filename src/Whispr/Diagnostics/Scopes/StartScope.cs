using System.Diagnostics;

namespace Whispr.Diagnostics.Scopes;

internal sealed class StartScope : IDisposable
{
    private readonly Activity _activity;
    private bool _disposed;

    public StartScope(Activity activity)
    {
        _activity = activity;
        _activity.DisplayName = "Whispr.Start";
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _activity.Dispose();
        _disposed = true;
    }
}
