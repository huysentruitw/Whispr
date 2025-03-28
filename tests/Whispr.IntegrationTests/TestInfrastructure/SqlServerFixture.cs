﻿using Testcontainers.MsSql;

namespace Whispr.IntegrationTests.TestInfrastructure;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container;

    // Since this is an assembly fixture, we need to use a static property to share the connection string
    public static string ConnectionString { get; private set; } = null!;

    public SqlServerFixture()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
            .WithPassword("Password123!")
            .Build();
    }

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        ConnectionString = _container.GetConnectionString()
            .Replace("Database=master", "Database=whispr-test");
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
