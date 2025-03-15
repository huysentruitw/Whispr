namespace Pigeon;

public sealed record Envelope<T>
    where T : class
{
    public required T Message { get; init; }

    public required string MessageType { get; init; }

    public Dictionary<string, string> Headers { get; init; } = new();

    public required string TopicName { get; init; }

    public required string CorrelationId { get; init; }
}
