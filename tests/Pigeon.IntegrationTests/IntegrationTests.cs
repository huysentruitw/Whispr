using Microsoft.EntityFrameworkCore;
using Pigeon.AzureServiceBus;
using Pigeon.EntityFrameworkCore;
using Pigeon.IntegrationTests.Tests.Conventions;
using Pigeon.IntegrationTests.Tests.Data;
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
        // harness.DataContext.Set<Product>().Add(new Product
        // {
        //     Id = Guid.NewGuid(),
        //     Name = "Test",
        //     Price = 1.23m
        // });

        // Act
        await harness.MessagePublisher.Publish(message, cancellationToken: TestContext.Current.CancellationToken);
        //// await harness.DataContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var handledMessage = ChirpHandler.WaitForMessage<ChirpHeard>(m => m.BirdName == "Robin", TimeSpan.FromSeconds(10));
        Assert.NotNull(handledMessage);
    }

    private sealed class TestHarness : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        private TestHarness(ServiceProvider serviceProvider, IMessagePublisher messagePublisher, DataContext dataContext)
        {
            _serviceProvider = serviceProvider;
            MessagePublisher = messagePublisher;
            DataContext = dataContext;
        }

        public ValueTask DisposeAsync() => _serviceProvider.DisposeAsync();

        public static async ValueTask<TestHarness> Create()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<IntegrationTests>()
                .Build();

            var services = new ServiceCollection()
                .AddDbContext<DataContext>(options =>
                {
                    var connectionString = configuration.GetValue<string>("SqlServer:ConnectionString")
                        ?? throw new InvalidOperationException("SqlServer:ConnectionString is required");
                    options.UseSqlServer(connectionString);
                })
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
                    // .AddEntityFrameworkCoreIntegration<DataContext>()
                .Services
                .BuildServiceProvider();

            await services.GetRequiredService<IMessageBusInitializer>().Start();
            var messagePublisher = services.GetRequiredService<IMessagePublisher>();
            var dataContext = services.GetRequiredService<DataContext>();
            return new TestHarness(services, messagePublisher, dataContext);
        }

        public IServiceProvider Services => _serviceProvider;

        public IMessagePublisher MessagePublisher { get; }

        public DataContext DataContext { get; }
    }
}
