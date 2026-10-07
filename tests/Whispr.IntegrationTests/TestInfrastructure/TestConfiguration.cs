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
}
