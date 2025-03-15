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

    public async ValueTask StartListener(string queueName, string[] topicNames, CancellationToken cancellationToken = default)
    {
        var subscriptionName = subscriptionNamingConvention.Format(queueName);
        await entityManager.CreateQueueIfNotExists(queueName, cancellationToken);
        foreach (var topicName in topicNames)
        {
            await entityManager.CreateTopicIfNotExists(topicName, cancellationToken);
            await entityManager.CreateSubscriptionIfNotExists(subscriptionName, topicName, queueName, cancellationToken);
        }

        var processor = processorFactory.GetOrCreateProcessor(queueName);

        processor.ProcessMessageAsync += e => Task.CompletedTask;
        processor.ProcessErrorAsync += e => Task.CompletedTask;

        await processor.StartProcessingAsync(cancellationToken);
    }
}
