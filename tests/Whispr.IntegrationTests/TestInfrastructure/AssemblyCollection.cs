using Whispr.IntegrationTests.TestInfrastructure;

[assembly: AssemblyFixture(typeof(SqlServerFixture))]
[assembly: AssemblyFixture(typeof(PostgreSqlFixture))]
[assembly: AssemblyFixture(typeof(RabbitMqFixture))]
[assembly: AssemblyFixture(typeof(HostFixture))]

public sealed class AssemblyMarker { }
