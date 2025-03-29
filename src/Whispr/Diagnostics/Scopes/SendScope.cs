using System.Diagnostics;

namespace Whispr.Diagnostics.Scopes;

internal sealed class SendScope(Activity activity) : IDisposable
{
    private bool _disposed;

    public const string ActivityName = "Whispr.Send";

    public void Dispose()
    {
        if (_disposed)
            return;

        activity.Dispose();
        _disposed = true;
    }
}
