using System.Diagnostics;
using Whispr.Diagnostics.Scopes;
using static Whispr.Diagnostics.WhisprActivitySource;

namespace Whispr.Diagnostics;

internal sealed class ActivityDiagnosticEventListener : IDiagnosticEventListener
{
    public IDisposable Start()
    {
        var activity = Source.CreateActivity(StartScope.ActivityName, ActivityKind.Internal);

        if (activity is null)
            return new EmptyScope();

        activity.Start();

        return new StartScope(activity);
    }

    public IDisposable Publish()
    {
        var activity = Source.CreateActivity(PublishScope.ActivityName, ActivityKind.Internal);

        if (activity is null)
            return new EmptyScope();

        activity.Start();

        return new PublishScope(activity);
    }

    public IDisposable Send()
    {
        var activity = Source.CreateActivity(SendScope.ActivityName, ActivityKind.Producer);

        if (activity is null)
            return new EmptyScope();

        activity.Start();

        return new SendScope(activity);
    }

    public IDisposable Consume()
    {
        var activity = Source.CreateActivity(ConsumeScope.ActivityName, ActivityKind.Consumer);

        if (activity is null)
            return new EmptyScope();

        activity.Start();

        return new ConsumeScope(activity);
    }
}
