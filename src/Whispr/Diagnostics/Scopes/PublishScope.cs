using System.Diagnostics;

namespace Whispr.Diagnostics.Scopes;

internal sealed class PublishScope : IDisposable
{
    private readonly Activity _activity;
    private bool _disposed;

    public PublishScope(Activity activity)
    {
        _activity = activity;
        _activity.DisplayName = "Whispr.Publish";
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _activity.Dispose();
        _disposed = true;
    }
}
