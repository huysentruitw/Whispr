using System.Diagnostics;

namespace Whispr.Diagnostics.Scopes;

internal sealed class PublishScope(Activity activity) : IDisposable
{
    private bool _disposed;

    public const string ActivityName = "Whispr.Publish";

    public PublishScope WithEnvelope<TMessage>(Envelope<TMessage> envelope)
        where TMessage : class
    {
        activity.DisplayName = $"Publishing {envelope.MessageType}";
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
