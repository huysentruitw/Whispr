using System.Diagnostics;

namespace Whispr.Diagnostics.Scopes;

internal sealed class ConsumeScope(Activity activity) : IDisposable
{
    private bool _disposed;

    public const string ActivityName = "Whispr.Consume";

    public void Dispose()
    {
        if (_disposed)
            return;

        activity.Dispose();
        _disposed = true;
    }
}
