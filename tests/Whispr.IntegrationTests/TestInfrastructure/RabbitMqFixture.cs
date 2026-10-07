using Testcontainers.RabbitMq;

namespace Whispr.IntegrationTests.TestInfrastructure;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4.1-management").Build();

    // Since this is an assembly fixture, we need to use a static property to share the connection string
    public static string ConnectionString { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        if (!TestConfiguration.UseRabbitMq)
            return;

        // Allows running against an existing broker, e.g. to inspect the queues afterward
        if (TestConfiguration.Configuration.GetValue<string>("RabbitMq:ConnectionString") is { Length: > 0 } connectionString)
        {
            ConnectionString = connectionString;
            return;
        }

        await _container.StartAsync();

        ConnectionString = _container.GetConnectionString();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
