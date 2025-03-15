namespace Pigeon.Filtering;

public interface ISendFilter
{
    ValueTask Send(SerializedEnvelope envelope, Func<SerializedEnvelope, CancellationToken, ValueTask> next, CancellationToken cancellationToken);
}
