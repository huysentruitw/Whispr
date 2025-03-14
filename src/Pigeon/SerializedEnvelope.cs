namespace Pigeon;

public sealed record SerializedEnvelope
{
    public required string Message { get; init; }
}
