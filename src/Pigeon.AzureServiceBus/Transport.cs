using Pigeon.AzureServiceBus.Conventions;
using Pigeon.AzureServiceBus.Factories;

namespace Pigeon.AzureServiceBus;

// TODO Check how we need to configure the service-bus entities
internal sealed class Transport(
    SenderFactory senderFactory,
    ProcessorFactory processorFactory,
    ServiceBusAdministrationClient administrationClient,
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
            var response = await administrationClient.CreateTopicAsync(envelope.TopicName, cancellationToken);
            if (!response.HasValue || response.Value.Status != EntityStatus.Active)
            {
                // TODO Throw explicit exception or log error
                throw;
            }

            await sender.SendMessageAsync(message, cancellationToken);
        }
    }

    public async ValueTask StartListener(string queueName, string[] topicNames, CancellationToken cancellationToken = default)
    {
        var subscriptionName = subscriptionNamingConvention.Format(queueName);
        await CreateQueueIfNotExists(queueName, cancellationToken);
        foreach (var topicName in topicNames)
        {
            await CreateTopicIfNotExists(topicName, cancellationToken);
            await CreateSubscriptionIfNotExists(subscriptionName, topicName, queueName, cancellationToken);
        }

        var processor = processorFactory.GetOrCreateProcessor(queueName);

        processor.ProcessMessageAsync += e => Task.CompletedTask;
        processor.ProcessErrorAsync += e => Task.CompletedTask;

        await processor.StartProcessingAsync(cancellationToken);
    }

    private async ValueTask CreateQueueIfNotExists(string queueName, CancellationToken cancellationToken)
    {
        if (await administrationClient.QueueExistsAsync(queueName, cancellationToken))
            return;

        var createResponse = await administrationClient.CreateQueueAsync(queueName, cancellationToken);
        if (createResponse.HasValue && createResponse.Value.Status == EntityStatus.Active)
            return;

        throw new InvalidOperationException($"Creation of queue '{queueName}' failed");
    }

    private async ValueTask CreateTopicIfNotExists(string topicName, CancellationToken cancellationToken)
    {
        if (await administrationClient.TopicExistsAsync(topicName, cancellationToken))
            return;

        var createResponse = await administrationClient.CreateTopicAsync(topicName, cancellationToken);
        if (createResponse.HasValue && createResponse.Value.Status == EntityStatus.Active)
            return;

        throw new InvalidOperationException($"Creation of topic '{topicName}' failed");
    }

    private async ValueTask CreateSubscriptionIfNotExists(string subscriptionName, string topicName, string queueName, CancellationToken cancellationToken)
    {
        if (await administrationClient.SubscriptionExistsAsync(topicName, subscriptionName, cancellationToken))
            return;

        var options = new CreateSubscriptionOptions(topicName, subscriptionName)
        {
            ForwardTo = queueName,
        };

        var createResponse = await administrationClient.CreateSubscriptionAsync(options, cancellationToken);
        if (createResponse.HasValue && createResponse.Value.Status == EntityStatus.Active)
            return;

        throw new InvalidOperationException($"Creation of subscription '{queueName}' on topic '{topicName}' failed");
    }
}
