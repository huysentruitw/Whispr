using Pigeon.AzureServiceBus;
using Pigeon.IntegrationTests.Tests.Conventions;
using Pigeon.IntegrationTests.Tests.Filters;
using Pigeon.IntegrationTests.Tests.Handlers;
using Pigeon.IntegrationTests.Tests.Messages;

namespace Pigeon.IntegrationTests.Tests;

public sealed class IntegrationTests
{
    [Fact]
    public async Task Given_MessageHandlerRegistered_When_MessagePublished_Then_MessageHandled()
    {
        // Arrange
        await using var harness = await TestHarness.Create();
        var message = new ChirpHeard("Robin", TimeSpan.FromSeconds(1));

        // Act
        await harness.MessagePublisher.Publish(message, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        var handledMessage = ChirpHandler.WaitForMessage<ChirpHeard>(m => m.BirdName == "Robin", TimeSpan.FromSeconds(30));
        Assert.NotNull(handledMessage);
    }

    private sealed class TestHarness : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private TestHarness(ServiceProvider serviceProvider, IMessagePublisher messagePublisher)
        {
            _serviceProvider = serviceProvider;
            MessagePublisher = messagePublisher;
        }

        public ValueTask DisposeAsync() => _serviceProvider.DisposeAsync();

        public static async ValueTask<TestHarness> Create()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<IntegrationTests>()
                .Build();

            var services = new ServiceCollection()
                .AddPigeon()
                    .AddAzureServiceBusTransport(options =>
                    {
                        options.ConnectionString =
                            configuration.GetValue<string>("AzureServiceBus:ConnectionString")
                            ?? throw new InvalidOperationException("AzureServiceBus:ConnectionString is required");
                    })
                    .AddTopicNamingConvention<TopicNamingConvention>()
                    .AddQueueNamingConvention<QueueNamingConvention>()
                    .AddSubscriptionNamingConvention<SubscriptionNamingConvention>()
                    .AddMessageHandlersFromAssembly(Assembly.GetExecutingAssembly())
                    .AddPublishFilter<FirstPublishFilter>()
                    .AddPublishFilter<SecondPublishFilter>()
                .Services
                .BuildServiceProvider();

            await services.GetRequiredService<IMessageBusInitializer>().Start();
            var messagePublisher = services.GetRequiredService<IMessagePublisher>();
            return new TestHarness(services, messagePublisher);
        }

        public IServiceProvider Services => _serviceProvider;

        public IMessagePublisher MessagePublisher { get; }
    }
}
