using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using Whispr.EntityFrameworkCore.Entities;
using Whispr.IntegrationTests.TestInfrastructure;
using Whispr.Transport;

namespace Whispr.IntegrationTests;

public sealed class OutboxConcurrencyTests
{
    private const int WritersPerHost = 4;
    private const int MessagesPerWriter = 25;

    [Fact]
    public async Task Given_SeveralProcessorsOnOneOutbox_When_AllPublishAtOnce_Then_EachMessageSentOnce()
    {
        // Arrange
        const int hostCount = 3;
        const int burstCount = 2;
        await RecreateOutboxTable<ConcurrentOutboxContext>();
        var transport = new CountingTransport();
        var hosts = new List<IHost>();

        try
        {
            await StartHosts<ConcurrentOutboxContext>(hosts, hostCount, transport);

            // Act
            for (var burst = 0; burst < burstCount; burst++)
            {
                await PublishFromAllHostsAtOnce<ConcurrentOutboxContext>(hosts);
                await WaitUntilOutboxDrained<ConcurrentOutboxContext>(TimeSpan.FromSeconds(60));
            }

            // Assert
            Assert.Equal(burstCount * hostCount * WritersPerHost * MessagesPerWriter, transport.SendCounts.Count);
            Assert.All(transport.SendCounts, sendCount => Assert.Equal(1, sendCount.Value));
        }
        finally
        {
            await StopHosts(hosts);
        }
    }

    [Fact]
    public async Task Given_OutboxWithRenamedKeyColumn_When_MessagesPublished_Then_EachMessageSentOnce()
    {
        // Arrange
        await RecreateOutboxTable<RenamedKeyOutboxContext>();
        var transport = new CountingTransport();
        var hosts = new List<IHost>();

        try
        {
            await StartHosts<RenamedKeyOutboxContext>(hosts, hostCount: 1, transport);

            // Act
            await PublishFromAllHostsAtOnce<RenamedKeyOutboxContext>(hosts);
            await WaitUntilOutboxDrained<RenamedKeyOutboxContext>(TimeSpan.FromSeconds(30));

            // Assert
            Assert.Equal(WritersPerHost * MessagesPerWriter, transport.SendCounts.Count);
            Assert.All(transport.SendCounts, sendCount => Assert.Equal(1, sendCount.Value));
        }
        finally
        {
            await StopHosts(hosts);
        }
    }

    private static async Task StartHosts<TContext>(List<IHost> hosts, int hostCount, CountingTransport transport)
        where TContext : DbContext
    {
        for (var i = 0; i < hostCount; i++)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddDbContext<TContext>(options => options.UseSqlServer(SqlServerFixture.ConnectionString));
                    services.AddKeyedSingleton<ITransport>(WhisprDefaults.DefaultBusName, transport);

                    services.AddWhispr()
                        .AddTopicNamingConvention<TopicNamingConvention>()
                        .AddQueueNamingConvention<QueueNamingConvention>()
                        .AddSubscriptionNamingConvention<SubscriptionNamingConvention>()
                        .AddOutbox<TContext>();
                })
                .Build();

            hosts.Add(host);
            await host.StartAsync(TestContext.Current.CancellationToken);
        }
    }

    private static async Task StopHosts(List<IHost> hosts)
    {
        foreach (var host in hosts)
        {
            await host.StopAsync(CancellationToken.None);
            host.Dispose();
        }
    }

    private static Task PublishFromAllHostsAtOnce<TContext>(List<IHost> hosts)
        where TContext : DbContext
    {
        var writers = hosts.SelectMany(host => Enumerable.Range(0, WritersPerHost).Select(async _ =>
        {
            for (var i = 0; i < MessagesPerWriter; i++)
            {
                await Task.Delay(Random.Shared.Next(0, 60), TestContext.Current.CancellationToken);

                await using var scope = host.Services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
                var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

                await messagePublisher.Publish(
                    new ChirpHeard(BirdId: Guid.NewGuid(), TimeUtc: DateTime.UtcNow),
                    cancellationToken: TestContext.Current.CancellationToken);
                await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
        }));

        return Task.WhenAll(writers);
    }

    private static async Task WaitUntilOutboxDrained<TContext>(TimeSpan timeout)
        where TContext : DbContext
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(timeout);

        await using var dbContext = CreateDbContext<TContext>();

        while (true)
        {
            try
            {
                if (!await dbContext.Set<OutboxMessage>().AnyAsync(m => m.ProcessedAtUtc == null && m.ParkedAtUtc == null, cts.Token))
                    return;
            }
            catch (SqlException ex) when (ex.Number == 1205)
            {
                // This poll can lose a deadlock to a processor marking messages as processed
            }

            await Task.Delay(100, cts.Token);
        }
    }

    // A fresh table, as SQL Server only scanned the outbox to mark messages as processed while it was small
    private static async Task RecreateOutboxTable<TContext>()
        where TContext : DbContext
    {
        await using var dbContext = CreateDbContext<TContext>();
        var dropTable = $"DROP TABLE IF EXISTS [{dbContext.Model.FindEntityType(typeof(OutboxMessage))!.GetTableName()}]";
        await dbContext.Database.ExecuteSqlRawAsync(dropTable, TestContext.Current.CancellationToken);
        await dbContext.GetService<IRelationalDatabaseCreator>().CreateTablesAsync(TestContext.Current.CancellationToken);
    }

    private static TContext CreateDbContext<TContext>()
        where TContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TContext>().UseSqlServer(SqlServerFixture.ConnectionString).Options;
        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }

    private sealed class ConcurrentOutboxContext(DbContextOptions<ConcurrentOutboxContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.AddOutboxMessageEntity(tableName: "ConcurrentOutboxMessage");
    }

    private sealed class RenamedKeyOutboxContext(DbContextOptions<RenamedKeyOutboxContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddOutboxMessageEntity(tableName: "RenamedKeyOutboxMessage");
            modelBuilder.Entity<OutboxMessage>().Property(x => x.Id).HasColumnName("OutboxMessageId");
        }
    }

    private sealed class CountingTransport : ITransport
    {
        public ConcurrentDictionary<string, int> SendCounts { get; } = new();

        public async ValueTask Send(string topicName, SerializedEnvelope envelope, CancellationToken cancellationToken = default)
        {
            // A broker round trip, so the processors' batches overlap in time
            await Task.Delay(Random.Shared.Next(10, 40), cancellationToken);
            SendCounts.AddOrUpdate(envelope.MessageId, 1, (_, count) => count + 1);
        }

        public ValueTask StartListener(
            string queueName,
            string[] topicNames,
            Func<SerializedEnvelope, CancellationToken, ValueTask> messageCallback,
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask StopListeners(CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}
