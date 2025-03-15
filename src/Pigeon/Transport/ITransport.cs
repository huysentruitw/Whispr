namespace Pigeon.Transport;

public interface ITransport
{
    ValueTask Send(SerializedEnvelope envelope, CancellationToken cancellationToken = default);

    ValueTask StartListener(
        string queueName,
        string[] topicNames,
        Func<SerializedEnvelope, CancellationToken, ValueTask> messageCallback,
        CancellationToken cancellationToken = default);
}
