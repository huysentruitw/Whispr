using Testcontainers.PostgreSql;

namespace Whispr.IntegrationTests.TestInfrastructure;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("whispr-test")
        .Build();

    // Since this is an assembly fixture, we need to use a static property to share the connection string
    public static string ConnectionString { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        if (TestConfiguration.SqlDialect != SqlDialect.PostgreSql)
            return;

        // Allows running against an existing server, e.g. to inspect the outbox afterward
        if (TestConfiguration.Configuration.GetValue<string>("PostgreSql:ConnectionString") is { Length: > 0 } connectionString)
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
