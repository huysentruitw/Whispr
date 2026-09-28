using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Whispr.AzureServiceBus.Management;

namespace Whispr.AzureServiceBus.Tests.Management;

public sealed class EntityManagerTests
{
    [Fact]
    public async Task Given_QueueCreationOptions_When_CreateQueueIfNotExists_Then_CreatesQueueWithOptions()
    {
        // Arrange
        var administrationClient = new Mock<ServiceBusAdministrationClient>();
        administrationClient
            .Setup(x => x.QueueExistsAsync("queue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        CreateQueueOptions? createOptions = null;
        administrationClient
            .Setup(x => x.CreateQueueAsync(It.IsAny<CreateQueueOptions>(), It.IsAny<CancellationToken>()))
            .Callback<CreateQueueOptions, CancellationToken>((options, _) => createOptions = options)
            .ReturnsAsync(CreateQueueResponse("queue"));

        var queueCreationOptions = new QueueCreationOptions
        {
            AutoDeleteOnIdle = TimeSpan.FromHours(1),
            DefaultMessageTimeToLive = TimeSpan.FromMinutes(30),
            LockDuration = TimeSpan.FromMinutes(1),
            MaxDeliveryCount = 3,
        };
        var entityManager = new EntityManager(administrationClient.Object, queueCreationOptions, new TopicCreationOptions());

        // Act
        await entityManager.CreateQueueIfNotExists("queue", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(createOptions);
        Assert.Equal(TimeSpan.FromHours(1), createOptions.AutoDeleteOnIdle);
        Assert.Equal(TimeSpan.FromMinutes(30), createOptions.DefaultMessageTimeToLive);
        Assert.Equal(TimeSpan.FromMinutes(1), createOptions.LockDuration);
        Assert.Equal(3, createOptions.MaxDeliveryCount);
    }

    [Fact]
    public async Task Given_TopicCreationOptions_When_CreateTopicIfNotExists_Then_CreatesTopicWithOptions()
    {
        // Arrange
        var administrationClient = new Mock<ServiceBusAdministrationClient>();
        administrationClient
            .Setup(x => x.TopicExistsAsync("topic", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        CreateTopicOptions? createOptions = null;
        administrationClient
            .Setup(x => x.CreateTopicAsync(It.IsAny<CreateTopicOptions>(), It.IsAny<CancellationToken>()))
            .Callback<CreateTopicOptions, CancellationToken>((options, _) => createOptions = options)
            .ReturnsAsync(CreateTopicResponse("topic"));

        var topicCreationOptions = new TopicCreationOptions
        {
            AutoDeleteOnIdle = TimeSpan.FromHours(1),
            DefaultMessageTimeToLive = TimeSpan.FromMinutes(30),
        };
        var entityManager = new EntityManager(administrationClient.Object, new QueueCreationOptions(), topicCreationOptions);

        // Act
        await entityManager.CreateTopicIfNotExists("topic", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(createOptions);
        Assert.Equal(TimeSpan.FromHours(1), createOptions.AutoDeleteOnIdle);
        Assert.Equal(TimeSpan.FromMinutes(30), createOptions.DefaultMessageTimeToLive);
    }

    // The model factory validates its arguments, so valid values are required even though they're not asserted
    private static Response<QueueProperties> CreateQueueResponse(string queueName)
        => Response.FromValue(
            ServiceBusModelFactory.QueueProperties(
                queueName,
                lockDuration: TimeSpan.FromMinutes(1),
                defaultMessageTimeToLive: TimeSpan.FromHours(1),
                autoDeleteOnIdle: TimeSpan.FromHours(1),
                duplicateDetectionHistoryTimeWindow: TimeSpan.FromMinutes(1),
                maxDeliveryCount: 1,
                userMetadata: string.Empty,
                status: EntityStatus.Active),
            Mock.Of<Response>());

    private static Response<TopicProperties> CreateTopicResponse(string topicName)
        => Response.FromValue(
            ServiceBusModelFactory.TopicProperties(
                topicName,
                defaultMessageTimeToLive: TimeSpan.FromHours(1),
                autoDeleteOnIdle: TimeSpan.FromHours(1),
                duplicateDetectionHistoryTimeWindow: TimeSpan.FromMinutes(1),
                status: EntityStatus.Active),
            Mock.Of<Response>());
}
