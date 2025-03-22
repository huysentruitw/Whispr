using Microsoft.Extensions.Hosting;

namespace Pigeon.IntegrationTests.Tests;

public sealed class IntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Given_MessageHandlerRegistered_When_MessagePublished_Then_MessageHandled(bool useOutbox)
    {
        // Arrange
        var message = new ChirpHeard($"Robin {Guid.NewGuid():N}", TimeSpan.FromSeconds(1));
        using var harness = await TestHarness.Create(useOutbox);

        // Act
        await MimicAction(harness.ServiceProvider, message);

        // Assert
        var handledMessage = ChirpHandler.WaitForMessage<ChirpHeard>(m => m.BirdName == message.BirdName, TimeSpan.FromSeconds(10));
        Assert.NotNull(handledMessage);
    }

    private static async ValueTask MimicAction(IServiceProvider serviceProvider, ChirpHeard message)
    {
        using var serviceScope = serviceProvider.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<DataContext>();
        var messagePublisher = serviceScope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        dbContext.Set<Product>().Add(new Product { Id = Guid.NewGuid(), Name = "Test", Price = 1.23m });
        await messagePublisher.Publish(message, cancellationToken: TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private sealed class TestHarness(IHost host) : IDisposable
    {
        public void Dispose() => host.Dispose();

        public static async ValueTask<TestHarness> Create(bool useOutbox)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<IntegrationTests>()
                .Build();

            var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(x => x.AddConfiguration(configuration))
                .ConfigureServices(services =>
                {
                    services
                        .AddDbContext<DataContext>(options =>
                        {
                            var connectionString = configuration.GetValue<string>("SqlServer:ConnectionString")
                                ?? throw new InvalidOperationException("SqlServer:ConnectionString is required");
                            options.UseSqlServer(connectionString);
                        });

                    var pigeonBuilder = services
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
                        .AddPublishFilter<SecondPublishFilter>();

                    if (useOutbox)
                    {
                        pigeonBuilder.AddOutbox<DataContext>(o =>
                        {
                            o.EnableMessageRetention = false;
                        });
                    }
                })
                .Build();

            await host.StartAsync();
            await host.Services.GetRequiredService<IMessageBusInitializer>().Start();

            return new TestHarness(host);
        }

        public IServiceProvider ServiceProvider => host.Services;
    }
}
