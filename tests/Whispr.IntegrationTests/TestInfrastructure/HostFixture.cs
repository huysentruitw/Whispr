using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Whispr.IntegrationTests.TestInfrastructure;

public sealed class HostFixture : IAsyncLifetime, IServiceProvider
{
    private IHost _host = null!;

    public async ValueTask InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<AssemblyMarker>()
            .AddEnvironmentVariables()
            .Build();

        SetupActivityListener();

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(x => x.AddConfiguration(configuration))
            .ConfigureServices(
                services =>
                {
                    services
                        .AddDbContext<DataContext>(
                            options =>
                            {
                                options.UseSqlServer(SqlServerFixture.ConnectionString);
                            });

                    services
                        .AddWhispr()
                        .AddAzureServiceBusTransport(
                            options =>
                            {
                                options.ConnectionString = configuration.GetValue<string>("AzureServiceBus:ConnectionString");
                                options.HostName = configuration.GetValue<string>("AzureServiceBus:HostName");
                                options.QueueConcurrencyLimit = Environment.ProcessorCount;
                            })
                        .AddTopicNamingConvention<TopicNamingConvention>()
                        .AddQueueNamingConvention<QueueNamingConvention>()
                        .AddSubscriptionNamingConvention<SubscriptionNamingConvention>()
                        .AddMessageHandlersFromAssembly(typeof(AssemblyMarker).Assembly)
                        .AddPublishFilter<FirstPublishFilter>()
                        .AddPublishFilter<SecondPublishFilter>()
                        .AddOutbox<DataContext>();
                })
            .Build();

        await RecreateDatabase(_host.Services);

        await _host.Services.GetRequiredService<IMessageBusInitializer>().Start();
        await _host.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
        _host = null!;
    }

    public object? GetService(Type serviceType) => _host.Services.GetService(serviceType);

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

    private static void SetupActivityListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        };

        ActivitySource.AddActivityListener(listener);
    }
}
