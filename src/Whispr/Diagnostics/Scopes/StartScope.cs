using System.Diagnostics;

namespace Whispr.Diagnostics.Scopes;

internal sealed class StartScope(Activity activity) : IDisposable
{
    private bool _disposed;

    public const string ActivityName = "Whispr.Start";

    public StartScope WithBusName(string busName)
    {
        activity.SetTag(TagNames.BusName, busName);
        return this;
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;

        activity.Dispose();
        _disposed = true;
    }
}
