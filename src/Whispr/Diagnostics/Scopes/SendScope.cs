using System.Diagnostics;

namespace Whispr.Diagnostics.Scopes;

internal sealed class SendScope(Activity activity) : IDisposable
{
    private bool _disposed;

    public const string ActivityName = "Whispr.Send";

    public SendScope WithTopicName(string topicName)
    {
        activity.SetTag(TagNames.TopicName, topicName);
        return this;
    }

    public SendScope WithEnvelope(SerializedEnvelope envelope)
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
