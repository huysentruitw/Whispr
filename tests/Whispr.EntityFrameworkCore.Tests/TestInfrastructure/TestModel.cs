using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Whispr.EntityFrameworkCore.Tests.TestInfrastructure;

internal static class TestModel
{
    public static IModel Build(SqlDialect sqlDialect, Action<ModelBuilder>? configure = null)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>()
            .ReplaceService<IModelCacheKeyFactory, UncachedModelCacheKeyFactory>();

        _ = sqlDialect switch
        {
            SqlDialect.MsSql => optionsBuilder.UseSqlServer("Server=unused"),
            SqlDialect.PostgreSql => optionsBuilder.UseNpgsql("Host=unused"),
            _ => throw new ArgumentOutOfRangeException(nameof(sqlDialect)),
        };

        using var context = new TestDbContext(
            optionsBuilder.Options,
            configure ?? (modelBuilder => modelBuilder.AddOutboxMessageEntity(sqlDialect: sqlDialect)));
        return context.Model;
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options, Action<ModelBuilder> configure) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => configure(modelBuilder);
    }

    // Each test builds its own model
    private sealed class UncachedModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime)
            => new object();
    }
}
