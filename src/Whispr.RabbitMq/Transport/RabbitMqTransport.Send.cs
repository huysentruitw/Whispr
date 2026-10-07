using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Exceptions;
using Whispr.RabbitMq.Management;

namespace Whispr.RabbitMq.Transport;

/// <inheritdoc />
internal sealed partial class RabbitMqTransport
{
    public async ValueTask Send(string topicName, SerializedEnvelope envelope, CancellationToken cancellationToken = default)
    {
        // A message deferred to a moment in the past, e.g. because it waited in the outbox, is sent right away
        var delaySeconds = envelope.DeferredUntil is { } deferredUntil
            ? DelayInfrastructure.GetDelaySeconds(deferredUntil, DateTimeOffset.UtcNow)
            : 0;

        if (delaySeconds > DelayInfrastructure.MaxDelaySeconds)
            throw new NotSupportedException($"The RabbitMQ transport does not support deferring messages more than {DelayInfrastructure.MaxDelaySeconds} seconds.");

        var properties = CreateBasicProperties(envelope);
        var body = Encoding.UTF8.GetBytes(envelope.Body);

        try
        {
            await Publish(topicName, delaySeconds, properties, body, cancellationToken);
        }
        catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == Constants.NotFound)
        {
            logger.LogInformation("Declaring exchange {TopicName} because it does not exist", topicName);
            topologyManager.ForgetTopic(topicName);

            // Retry publishing the message, which declares the exchange again
            await Publish(topicName, delaySeconds, properties, body, cancellationToken);
        }
    }

    private ValueTask Publish(string topicName, long delaySeconds, BasicProperties properties, byte[] body, CancellationToken cancellationToken)
    {
        return publishChannelPool.Use(
            async channel =>
            {
                await topologyManager.DeclareExchangeIfNotDeclared(channel, topicName, cancellationToken);

                var exchange = topicName;
                var routingKey = string.Empty;

                if (delaySeconds > 0)
                {
                    await topologyManager.DeclareDelayInfrastructureIfNotDeclared(channel, topicName, cancellationToken);
                    exchange = DelayInfrastructure.EntryExchangeName;
                    routingKey = DelayInfrastructure.GetRoutingKey(delaySeconds, topicName);
                }

                // Completes when the broker confirmed the message, so the outbox only marks it as sent afterward
                await channel.BasicPublishAsync(exchange, routingKey, mandatory: false, properties, body, cancellationToken);
            },
            cancellationToken);
    }

    private static BasicProperties CreateBasicProperties(SerializedEnvelope envelope)
    {
        var headers = new Dictionary<string, object?> { [MessageTypeHeaderName] = envelope.MessageType };

        if (envelope.DeferredUntil.HasValue)
            headers[DeferredUntilHeaderName] = envelope.DeferredUntil.Value.ToString("O", CultureInfo.InvariantCulture);

        return new BasicProperties
        {
            ContentType = ContentType,
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            Headers = headers,
        };
    }
}
