using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Exceptions;

namespace Whispr.RabbitMq.Transport;

/// <inheritdoc />
internal sealed partial class RabbitMqTransport
{
    public async ValueTask Send(string topicName, SerializedEnvelope envelope, CancellationToken cancellationToken = default)
    {
        // A message deferred to a moment in the past, e.g. because it waited in the outbox, can be sent right away
        if (envelope.DeferredUntil > DateTimeOffset.UtcNow)
            throw new NotSupportedException("The RabbitMQ transport does not support deferred messages.");

        var properties = CreateBasicProperties(envelope);
        var body = Encoding.UTF8.GetBytes(envelope.Body);

        try
        {
            await Publish(topicName, properties, body, cancellationToken);
        }
        catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == Constants.NotFound)
        {
            logger.LogInformation("Declaring exchange {TopicName} because it does not exist", topicName);
            topologyManager.ForgetExchange(topicName);

            // Retry publishing the message, which declares the exchange again
            await Publish(topicName, properties, body, cancellationToken);
        }
    }

    private ValueTask Publish(string topicName, BasicProperties properties, byte[] body, CancellationToken cancellationToken)
    {
        return publishChannelPool.Use(
            async channel =>
            {
                await topologyManager.DeclareExchangeIfNotDeclared(channel, topicName, cancellationToken);

                // Completes when the broker confirmed the message, so the outbox only marks it as sent afterward
                await channel.BasicPublishAsync(topicName, routingKey: string.Empty, mandatory: false, properties, body, cancellationToken);
            },
            cancellationToken);
    }

    private static BasicProperties CreateBasicProperties(SerializedEnvelope envelope)
    {
        return new BasicProperties
        {
            ContentType = ContentType,
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            Headers = new Dictionary<string, object?> { [MessageTypeHeaderName] = envelope.MessageType },
        };
    }
}
