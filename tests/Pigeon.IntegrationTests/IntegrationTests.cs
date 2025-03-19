using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
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
        using var harness = await TestHarness.Create();
        var message = new ChirpHeard("Robin", TimeSpan.FromSeconds(1));
        harness.DataContext.Set<Product>().Add(new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            Price = 1.23m
        });

        // Act
        await harness.MessagePublisher.Publish(message, cancellationToken: TestContext.Current.CancellationToken);
        await harness.DataContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var handledMessage = ChirpHandler.WaitForMessage<ChirpHeard>(m => m.BirdName == "Robin", TimeSpan.FromSeconds(10));
        Assert.NotNull(handledMessage);
    }

    private sealed class TestHarness(IHost host) : IDisposable
    {
        private readonly IServiceScope _scope = host.Services.CreateScope();

        public void Dispose() => host.Dispose();

        public static async ValueTask<TestHarness> Create()
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
                        .AddOutboxSendFilter<DataContext>();
                })
                .Build();

            await host.StartAsync();
            await host.Services.GetRequiredService<IMessageBusInitializer>().Start();

            return new TestHarness(host);
        }

        public IMessagePublisher MessagePublisher => _scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        public DataContext DataContext => _scope.ServiceProvider.GetRequiredService<DataContext>();
    }
}
