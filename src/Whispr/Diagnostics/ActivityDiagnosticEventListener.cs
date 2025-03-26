using Whispr.Diagnostics.Scopes;
using static Whispr.Diagnostics.WhisprActivitySource;

namespace Whispr.Diagnostics;

internal sealed class ActivityDiagnosticEventListener : IDiagnosticEventListener
{
    public IDisposable Start()
    {
        var activity = Source.StartActivity();

        if (activity is null)
            return new EmptyScope();

        return new StartScope(activity);
    }

    public IDisposable Publish()
    {
        var activity = Source.StartActivity();

        if (activity is null)
            return new EmptyScope();

        return new PublishScope(activity);
    }

    public IDisposable Send()
    {
        var activity = Source.StartActivity();

        if (activity is null)
            return new EmptyScope();

        return new SendScope(activity);
    }

    public IDisposable Consume()
    {
        var activity = Source.StartActivity();

        if (activity is null)
            return new EmptyScope();

        return new ConsumeScope(activity);
    }
}
