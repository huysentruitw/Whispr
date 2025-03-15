namespace Pigeon;

public sealed record SerializedEnvelope
{
    public required string Body { get; init; }

    public required string MessageType { get; init; }

    public required string TopicName { get; init; }

    public required string CorrelationId { get; init; }
}
