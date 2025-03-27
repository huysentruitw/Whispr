using Microsoft.Extensions.Hosting;

namespace Whispr.IntegrationTests.Tests.TestInfrastructure;

public static class HostFactory
{
    public static async ValueTask<IHost> Create(string sqlConnectionString, bool useOutbox)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<IntegrationTests>()
            .AddEnvironmentVariables()
            .Build();

        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(x => x.AddConfiguration(configuration))
            .ConfigureServices(
                services =>
                {
                    services
                        .AddDbContext<DataContext>(
                            options =>
                            {
                                options.UseSqlServer(sqlConnectionString);
                            });

                    var builder = services
                        .AddWhispr()
                        .AddAzureServiceBusTransport(
                            options =>
                            {
                                var connectionString = configuration.GetValue<string>("AzureServiceBus:ConnectionString");
                                var hostAddress = configuration.GetValue<string>("AzureServiceBus:HostAddress");

                                if (string.IsNullOrEmpty(hostAddress) && string.IsNullOrEmpty(connectionString))
                                    throw new InvalidOperationException("Either AzureServiceBus:ConnectionString or AzureServiceBus:HostAddress is required");

                                options.ConnectionString = connectionString;
                                options.HostAddress = hostAddress;
                            })
                        .AddTopicNamingConvention<TopicNamingConvention>()
                        .AddQueueNamingConvention<QueueNamingConvention>()
                        .AddSubscriptionNamingConvention<SubscriptionNamingConvention>()
                        .AddMessageHandlersFromAssembly(Assembly.GetExecutingAssembly())
                        .AddPublishFilter<FirstPublishFilter>()
                        .AddPublishFilter<SecondPublishFilter>();

                    if (useOutbox)
                        builder.AddOutbox<DataContext>();
                })
            .Build();

        await RecreateDatabase(host.Services);

        await host.StartAsync();
        await host.Services.GetRequiredService<IMessageBusInitializer>().Start();

        return host;
    }

    private static async Task RecreateDatabase(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

        EnsureTestDatabase(dbContext);

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    private static void EnsureTestDatabase(DataContext dbContext)
    {
        var databaseName = dbContext.Database.GetDbConnection().Database;
        if (!databaseName.EndsWith("-test"))
            throw new InvalidOperationException("Not a test database!");
    }
}