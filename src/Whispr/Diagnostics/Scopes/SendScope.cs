using System.Diagnostics;

namespace Whispr.Diagnostics.Scopes;

internal sealed class SendScope : IDisposable
{
    private readonly Activity _activity;
    private bool _disposed;

    public SendScope(Activity activity)
    {
        _activity = activity;
        _activity.DisplayName = "Whispr.Send";
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _activity.Dispose();
        _disposed = true;
    }
}
