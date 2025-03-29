using Whispr.Diagnostics.Scopes;
using Whispr.EntityFrameworkCore.Diagnostics.Scopes;
using static Whispr.Diagnostics.WhisprActivitySource;

namespace Whispr.EntityFrameworkCore.Diagnostics;

internal sealed class ActivityDiagnosticEventListener : IDiagnosticEventListener
{
    public IDisposable ProcessOutboxMessage(OutboxMessage outboxMessage)
    {
        ActivityContext.TryParse(outboxMessage.TraceParent, null, out var parentContext);

        var activity = Source.CreateActivity(
            name: ProcessOutboxMessageScope.ActivityName,
            kind: ActivityKind.Client,
            parentContext: parentContext);

        if (activity is null)
            return new EmptyScope();

        activity.Start();

        return new ProcessOutboxMessageScope(activity)
            .WithMessageId(outboxMessage.MessageId)
            .WithMessageType(outboxMessage.MessageType);
    }
}
