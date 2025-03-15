namespace Pigeon.Transport;

public interface ITransport
{
    ValueTask Send(SerializedEnvelope envelope, CancellationToken cancellationToken = default);

    ValueTask StartListener(string queueName, string[] topicNames, CancellationToken cancellationToken = default);
}
