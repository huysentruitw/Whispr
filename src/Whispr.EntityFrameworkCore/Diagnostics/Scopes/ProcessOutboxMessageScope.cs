using Whispr.Diagnostics;

namespace Whispr.EntityFrameworkCore.Diagnostics.Scopes;

internal sealed class ProcessOutboxMessageScope(Activity activity) : IDisposable
{
    private bool _disposed;

    public const string ActivityName = "Whispr.ProcessOutboxMessage";

    public ProcessOutboxMessageScope WithMessageId(string messageId)
    {
        activity.SetTag(TagNames.MessageId, messageId);
        return this;
    }

    public ProcessOutboxMessageScope WithMessageType(string messageType)
    {
        activity.SetTag(TagNames.MessageType, messageType);
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
