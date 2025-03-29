using System.Diagnostics;

namespace Whispr.Diagnostics.Scopes;

internal sealed class ConsumeScope(Activity activity) : IDisposable
{
    private bool _disposed;

    public const string ActivityName = "Whispr.Consume";

    public ConsumeScope WithQueueName(string queueName)
    {
        activity.SetTag(TagNames.QueueName, queueName);
        return this;
    }

    public ConsumeScope WithEnvelope(SerializedEnvelope envelope)
    {
        activity.SetTag(TagNames.MessageId, envelope.MessageId);
        activity.SetTag(TagNames.MessageType, envelope.MessageType);
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
