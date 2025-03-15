using System.Text;
using System.Text.Json;
using Pigeon.AzureServiceBus.Conventions;
using Pigeon.AzureServiceBus.Factories;

namespace Pigeon.AzureServiceBus;

internal sealed class Transport(
    SenderFactory senderFactory,
    ProcessorFactory processorFactory,
    EntityManager entityManager,
    ISubscriptionNamingConvention subscriptionNamingConvention) : ITransport
{
    public async ValueTask Send(SerializedEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var sender = senderFactory.GetOrCreateSender(envelope.TopicName);

        var message = new ServiceBusMessage(envelope.Body)
        {
            CorrelationId = envelope.CorrelationId,
        };

        try
        {
            await sender.SendMessageAsync(message, cancellationToken);
        }
        catch (ServiceBusException ex) when(ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            await entityManager.CreateTopicIfNotExists(envelope.TopicName, cancellationToken);
            await sender.SendMessageAsync(message, cancellationToken);
        }
    }

    public async ValueTask StartListener(string queueName,
        string[] topicNames,
        Func<SerializedEnvelope, CancellationToken, ValueTask> messageCallback,
        CancellationToken cancellationToken = default)
    {
        var subscriptionName = subscriptionNamingConvention.Format(queueName);
        await entityManager.CreateQueueIfNotExists(queueName, cancellationToken);
        foreach (var topicName in topicNames)
        {
            await entityManager.CreateTopicIfNotExists(topicName, cancellationToken);
            await entityManager.CreateSubscriptionIfNotExists(subscriptionName, topicName, queueName, cancellationToken);
        }

        var processor = processorFactory.GetOrCreateProcessor(queueName);

        processor.ProcessMessageAsync += async args =>
        {
            var messageBody = Encoding.UTF8.GetString(args.Message.Body);
            var envelopeMetadata = JsonSerializer.Deserialize<EnvelopeMetadata>(messageBody)
                ?? throw new InvalidOperationException($"Failed to deserialize message with correlation ID: {args.Message.CorrelationId}");

            var serializedEnvelope = new SerializedEnvelope
            {
                Body = messageBody,
                MessageType = envelopeMetadata.MessageType,
                TopicName = envelopeMetadata.TopicName,
                CorrelationId = args.Message.CorrelationId,
            };

            try
            {
                await messageCallback(serializedEnvelope, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to process message with correlation ID: {args.Message.CorrelationId}", ex);
            }

            await args.CompleteMessageAsync(args.Message, cancellationToken);
        };

        // TODO Implement error handling
        processor.ProcessErrorAsync += args => Task.CompletedTask;

        await processor.StartProcessingAsync(cancellationToken);
    }

    private sealed record EnvelopeMetadata : EnvelopeBase;
}
