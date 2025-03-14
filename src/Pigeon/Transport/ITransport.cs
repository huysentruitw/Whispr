namespace Pigeon.Transport;

public interface ITransport
{
    Task Send(SerializedEnvelope envelope, CancellationToken cancellationToken = default);
}
