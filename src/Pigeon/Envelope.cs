namespace Pigeon;

public sealed record Envelope<T> : EnvelopeBase
    where T : class
{
    public required T Message { get; init; }

    public Dictionary<string, string> Headers { get; init; } = new();
}

public abstract record EnvelopeBase
{
    public required string MessageType { get; init; }

    public required string TopicName { get; init; }

    public required string CorrelationId { get; init; }

    public required DateTimeOffset? DeferredUntil { get; init; }
}
