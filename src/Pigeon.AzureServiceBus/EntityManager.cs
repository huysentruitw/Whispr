using System.Collections.Concurrent;

namespace Pigeon.AzureServiceBus;

// TODO Check how we need to configure the service-bus entities
internal sealed class EntityManager(ServiceBusAdministrationClient administrationClient)
{
    private readonly ConcurrentDictionary<string, object> _entitySyncRoots = new();

    public async Task CreateQueueIfNotExists(string queueName, CancellationToken cancellationToken = default)
    {
        if (!await administrationClient.QueueExistsAsync(queueName, cancellationToken))
            await CreateQueue(queueName, cancellationToken);
    }

    public async Task CreateQueue(string queueName, CancellationToken cancellationToken = default)
    {
        try
        {
            var createResponse = await administrationClient.CreateQueueAsync(queueName, cancellationToken);
            if (createResponse.HasValue && createResponse.Value.Status == EntityStatus.Active)
                return;
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
        {
            return;
        }

        throw new InvalidOperationException($"Creation of queue '{queueName}' failed");
    }

    public async Task CreateTopicIfNotExists(string topicName, CancellationToken cancellationToken = default)
    {
        if (!await administrationClient.TopicExistsAsync(topicName, cancellationToken))
            await CreateTopic(topicName, cancellationToken);
    }

    public async Task CreateTopic(string topicName, CancellationToken cancellationToken = default)
    {
        try
        {
            var createResponse = await administrationClient.CreateTopicAsync(topicName, cancellationToken);
            if (createResponse.HasValue && createResponse.Value.Status == EntityStatus.Active)
                return;
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
        {
            return;
        }

        throw new InvalidOperationException($"Creation of topic '{topicName}' failed");
    }

    public async Task CreateSubscriptionIfNotExists(string subscriptionName, string topicName, string queueName, CancellationToken cancellationToken = default)
    {
        if (!await administrationClient.SubscriptionExistsAsync(topicName, subscriptionName, cancellationToken))
            await CreateSubscription(subscriptionName, topicName, queueName, cancellationToken);
    }

    public async Task CreateSubscription(string subscriptionName, string topicName, string queueName, CancellationToken cancellationToken = default)
    {
        try
        {
            var createOptions = new CreateSubscriptionOptions(topicName, subscriptionName)
            {
                ForwardTo = queueName,
            };

            var createResponse = await administrationClient.CreateSubscriptionAsync(createOptions, cancellationToken);
            if (createResponse.HasValue && createResponse.Value.Status == EntityStatus.Active)
                return;
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
        {
            return;
        }

        throw new InvalidOperationException($"Creation of subscription '{subscriptionName}' failed");
    }
}
