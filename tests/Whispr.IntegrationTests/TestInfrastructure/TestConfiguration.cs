namespace Whispr.IntegrationTests.TestInfrastructure;

public static class TestConfiguration
{
    private static readonly Lazy<IConfiguration> LazyConfiguration = new(
        () => new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<AssemblyMarker>()
            .AddEnvironmentVariables()
            .Build());

    public static IConfiguration Configuration => LazyConfiguration.Value;

    public static bool UseRabbitMq => string.Equals(Configuration.GetValue<string>("Transport"), "RabbitMq", StringComparison.OrdinalIgnoreCase);

    public static SqlDialect SqlDialect => string.Equals(Configuration.GetValue<string>("Database"), "PostgreSql", StringComparison.OrdinalIgnoreCase)
        ? SqlDialect.PostgreSql
        : SqlDialect.MsSql;

    public static string ConnectionString => SqlDialect == SqlDialect.PostgreSql
        ? PostgreSqlFixture.ConnectionString
        : SqlServerFixture.ConnectionString;

    public static DbContextOptionsBuilder UseTestDatabase(this DbContextOptionsBuilder optionsBuilder)
        => SqlDialect == SqlDialect.PostgreSql
            ? optionsBuilder.UseNpgsql(ConnectionString)
            : optionsBuilder.UseSqlServer(ConnectionString);

    public static DbContextOptionsBuilder<TContext> UseTestDatabase<TContext>(this DbContextOptionsBuilder<TContext> optionsBuilder)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)((DbContextOptionsBuilder)optionsBuilder).UseTestDatabase();
}
