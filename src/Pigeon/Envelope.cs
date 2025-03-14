namespace Pigeon;

public sealed record Envelope<T>
    where T : class
{
    public required T Message { get; init; }
}
