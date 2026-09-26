namespace Whispr.AzureServiceBus.Management;

internal sealed class EntityManager(
    ServiceBusAdministrationClient administrationClient,
    QueueCreationOptions queueCreationOptions,
    TopicCreationOptions topicCreationOptions)
{
    public async Task CreateQueueIfNotExists(string queueName, CancellationToken cancellationToken = default)
    {
        if (!await administrationClient.QueueExistsAsync(queueName, cancellationToken))
            await CreateQueue(queueName, cancellationToken);
    }

    private async Task CreateQueue(string queueName, CancellationToken cancellationToken = default)
    {
        try
        {
            var createOptions = new CreateQueueOptions(queueName)
            {
                AutoDeleteOnIdle = queueCreationOptions.AutoDeleteOnIdle,
                DefaultMessageTimeToLive = queueCreationOptions.DefaultMessageTimeToLive,
                EnableBatchedOperations = true,
                DeadLetteringOnMessageExpiration = true,
                LockDuration = queueCreationOptions.LockDuration,
                MaxDeliveryCount = queueCreationOptions.MaxDeliveryCount,
            };

            var createResponse = await administrationClient.CreateQueueAsync(createOptions, cancellationToken);
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

    private async Task CreateTopic(string topicName, CancellationToken cancellationToken = default)
    {
        try
        {
            var createOptions = new CreateTopicOptions(topicName)
            {
                AutoDeleteOnIdle = topicCreationOptions.AutoDeleteOnIdle,
                DefaultMessageTimeToLive = topicCreationOptions.DefaultMessageTimeToLive,
                EnableBatchedOperations = true,
            };

            var createResponse = await administrationClient.CreateTopicAsync(createOptions, cancellationToken);
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

    private async Task CreateSubscription(string subscriptionName, string topicName, string queueName, CancellationToken cancellationToken = default)
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
