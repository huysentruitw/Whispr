using Microsoft.EntityFrameworkCore.Metadata;
using Whispr.EntityFrameworkCore.Entities;
using Whispr.EntityFrameworkCore.Tests.TestInfrastructure;

namespace Whispr.EntityFrameworkCore.Tests;

public sealed class ModelBuilderExtensionsTests
{
    [Fact]
    public void Given_DefaultDialect_When_AddOutboxMessageEntity_Then_PendingIndexFilterUsesMsSqlQuoting()
    {
        // Arrange & Act
        var model = TestModel.Build(SqlDialect.MsSql, modelBuilder => modelBuilder.AddOutboxMessageEntity());

        // Assert
        Assert.Equal("[ProcessedAtUtc] IS NULL AND [ParkedAtUtc] IS NULL", GetPendingIndexFilter(model));
    }

    [Fact]
    public void Given_PostgreSqlDialect_When_AddOutboxMessageEntity_Then_PendingIndexFilterUsesPostgreSqlQuoting()
    {
        // Arrange & Act
        var model = TestModel.Build(SqlDialect.PostgreSql, modelBuilder => modelBuilder.AddOutboxMessageEntity(sqlDialect: SqlDialect.PostgreSql));

        // Assert
        Assert.Equal("\"ProcessedAtUtc\" IS NULL AND \"ParkedAtUtc\" IS NULL", GetPendingIndexFilter(model));
    }

    [Fact]
    public void Given_ColumnNamesFromConvention_When_AddOutboxMessageEntity_Then_PendingIndexFilterUsesThoseNames()
    {
        // Arrange & Act
        var model = TestModel.Build(SqlDialect.PostgreSql, modelBuilder =>
        {
            // Mimics a naming convention, which maps the column names before the outbox configuration is applied
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.Property(x => x.ProcessedAtUtc).HasColumnName("processed_at_utc");
                entity.Property(x => x.ParkedAtUtc).HasColumnName("parked_at_utc");
            });
            modelBuilder.AddOutboxMessageEntity(sqlDialect: SqlDialect.PostgreSql);
        });

        // Assert
        Assert.Equal("\"processed_at_utc\" IS NULL AND \"parked_at_utc\" IS NULL", GetPendingIndexFilter(model));
    }

    private static string? GetPendingIndexFilter(IModel model)
        => model.FindEntityType(typeof(OutboxMessage))!
            .GetIndexes()
            .Single(index => index.Properties.Select(p => p.Name).SequenceEqual([nameof(OutboxMessage.CreatedAtUtc)]))
            .GetFilter();
}
