using Pigeon.AzureServiceBus.Tests.Conventions;
using Pigeon.AzureServiceBus.Tests.Messages;

namespace Pigeon.AzureServiceBus.Tests;

public sealed class IntegrationTests
{
    [Fact]
    public async Task Test()
    {
        // Arrange
        await using var harness = await TestHarness.Create();
        var message = new ChirpHeard("Robin", TimeSpan.FromSeconds(1));

        // Act
        await harness.MessageBus.Publish(message, TestContext.Current.CancellationToken);

        // Assert
        // await Task.Delay(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
    }

    private sealed class TestHarness : IAsyncDisposable
    {
        private TestHarness(ServiceProvider serviceProvider, IMessageBus messageBus)
        {
            ServiceProvider = serviceProvider;
            MessageBus = messageBus;
        }

        public ValueTask DisposeAsync() => ServiceProvider.DisposeAsync();

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
                .Services
                .BuildServiceProvider();

            var messageBus = services.GetRequiredService<IMessageBus>();
            await messageBus.Start();
            return new TestHarness(services, messageBus);
        }

        public ServiceProvider ServiceProvider { get; }

        public IMessageBus MessageBus { get; }
    }
}
