namespace Pigeon.Filtering;

public interface ISendFilter
{
    Task Send(SerializedEnvelope envelope, Func<SerializedEnvelope, CancellationToken, Task> next, CancellationToken cancellationToken);
}
