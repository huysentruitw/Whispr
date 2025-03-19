using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pigeon.EntityFrameworkCore;

namespace Pigeon.IntegrationTests.Tests.Data;

public sealed class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly)
            .AddOutboxMessageEntity(schemaName: "Application");
    }
}

public sealed class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<IntegrationTests>()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        optionsBuilder.UseSqlServer(configuration.GetValue<string>("SqlServer:ConnectionString"));
        return new DataContext(optionsBuilder.Options);
    }
}
