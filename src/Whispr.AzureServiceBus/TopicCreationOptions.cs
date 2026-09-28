namespace Whispr.AzureServiceBus;

/// <summary>
/// Options applied when Whispr creates a topic. Existing topics are not updated.
/// </summary>
public sealed record TopicCreationOptions
{
    /// <summary>
    /// The idle interval after which the topic is automatically deleted.
    /// </summary>
    public TimeSpan AutoDeleteOnIdle { get; set; } = TimeSpan.FromDays(427);

    /// <summary>
    /// The default time to live of messages in the topic.
    /// </summary>
    public TimeSpan DefaultMessageTimeToLive { get; set; } = TimeSpan.FromDays(365);
}
