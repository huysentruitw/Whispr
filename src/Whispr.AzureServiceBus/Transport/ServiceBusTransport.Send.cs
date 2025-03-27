using Microsoft.Extensions.Logging;

namespace Whispr.AzureServiceBus.Transport;

/// <inheritdoc />
internal sealed partial class ServiceBusTransport
{
    public async ValueTask Send(string topicName, SerializedEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var sender = senderFactory.GetOrCreateSender(topicName);
        var message = CreateServiceBusMessage(envelope);

        try
        {
            await sender.SendMessageAsync(message, cancellationToken);
        }
        catch (ServiceBusException ex) when(ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            logger.LogInformation("Creating topic {TopicName} because it does not exist", topicName);
            await entityManager.CreateTopicIfNotExists(topicName, cancellationToken);

            // Retry sending the message after creating the topic
            await sender.SendMessageAsync(message, cancellationToken);
        }
    }

    private static ServiceBusMessage CreateServiceBusMessage(SerializedEnvelope envelope)
    {
        var message = new ServiceBusMessage(envelope.Body)
        {
            ContentType = "application/json",
            ApplicationProperties = { [MessageTypePropertyName] = envelope.MessageType },
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
        };

        if (envelope.DeferredUntil.HasValue)
            message.ScheduledEnqueueTime = envelope.DeferredUntil.Value;

        return message;
    }
}
