using System.Diagnostics;

namespace Whispr.Diagnostics.Scopes;

internal sealed class PublishScope(Activity activity) : IDisposable
{
    private bool _disposed;

    public const string ActivityName = "Whispr.Publish";

    public void Dispose()
    {
        if (_disposed)
            return;

        activity.Dispose();
        _disposed = true;
    }
}
