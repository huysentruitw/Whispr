using Microsoft.EntityFrameworkCore.Metadata;
using Whispr.EntityFrameworkCore.Entities;
using Whispr.EntityFrameworkCore.Processing.SqlStatementBuilders;
using Whispr.EntityFrameworkCore.Tests.TestInfrastructure;

namespace Whispr.EntityFrameworkCore.Tests.Processing.SqlStatementBuilders;

public sealed class PostgreSqlStatementsBuilderTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private static readonly DateTimeOffset ProcessedAtUtc = new(2026, 1, 2, 3, 4, 6, TimeSpan.Zero);

    [Fact]
    public void Given_DefaultModel_When_BuildSelectBatch_Then_ReturnsLockingSelect()
    {
        // Arrange
        var builder = PostgreSqlStatementsBuilder.Create(BuildModel());

        // Act
        var statement = builder.BuildSelectBatch(maxMessageBatchSize: 50, NowUtc);

        // Assert
        Assert.Equal(
            """
            SELECT * FROM "OutboxMessage"
            WHERE "ProcessedAtUtc" IS NULL AND "ParkedAtUtc" IS NULL AND ("NextAttemptAtUtc" IS NULL OR "NextAttemptAtUtc" <= {0})
            ORDER BY "CreatedAtUtc"
            LIMIT 50
            FOR UPDATE SKIP LOCKED
            """,
            statement.Sql);
        Assert.Equal(new object[] { NowUtc }, statement.Parameters);
    }

    [Fact]
    public void Given_RenamedTableAndColumns_When_BuildSelectBatch_Then_UsesNamesFromModel()
    {
        // Arrange
        var model = BuildModel(modelBuilder =>
        {
            modelBuilder.AddOutboxMessageEntity(tableName: "outbox", schemaName: "messaging", sqlDialect: SqlDialect.PostgreSql);
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
                entity.Property(x => x.ProcessedAtUtc).HasColumnName("processed_at_utc");
                entity.Property(x => x.ParkedAtUtc).HasColumnName("parked_at_utc");
                entity.Property(x => x.NextAttemptAtUtc).HasColumnName("next_attempt_at_utc");
            });
        });
        var builder = PostgreSqlStatementsBuilder.Create(model);

        // Act
        var statement = builder.BuildSelectBatch(maxMessageBatchSize: 50, NowUtc);

        // Assert
        Assert.Equal(
            """
            SELECT * FROM "messaging"."outbox"
            WHERE "processed_at_utc" IS NULL AND "parked_at_utc" IS NULL AND ("next_attempt_at_utc" IS NULL OR "next_attempt_at_utc" <= {0})
            ORDER BY "created_at_utc"
            LIMIT 50
            FOR UPDATE SKIP LOCKED
            """,
            statement.Sql);
    }

    [Fact]
    public void Given_NameWithDoubleQuote_When_BuildSelectBatch_Then_NameIsEscaped()
    {
        // Arrange
        var model = BuildModel(modelBuilder => modelBuilder.AddOutboxMessageEntity(tableName: "Out\"box", sqlDialect: SqlDialect.PostgreSql));
        var builder = PostgreSqlStatementsBuilder.Create(model);

        // Act
        var statement = builder.BuildSelectBatch(maxMessageBatchSize: 50, NowUtc);

        // Assert
        Assert.StartsWith("SELECT * FROM \"Out\"\"box\"", statement.Sql);
    }

    [Fact]
    public void Given_Ids_When_BuildMarkAsProcessed_Then_UpdatesWithIdArray()
    {
        // Arrange
        var model = BuildModel(modelBuilder =>
        {
            modelBuilder.AddOutboxMessageEntity(sqlDialect: SqlDialect.PostgreSql);
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.Property(x => x.Id).HasColumnName("OutboxMessageId");
                entity.Property(x => x.ProcessedAtUtc).HasColumnName("Processed");
            });
        });
        var builder = PostgreSqlStatementsBuilder.Create(model);
        long[] ids = [10, 11];

        // Act
        var statement = Assert.Single(builder.BuildMarkAsProcessed(ids, ProcessedAtUtc));

        // Assert
        Assert.Equal(
            """UPDATE "OutboxMessage" SET "Processed" = {0} WHERE "OutboxMessageId" = ANY({1})""",
            statement.Sql);
        Assert.Equal(new object[] { ProcessedAtUtc, ids }, statement.Parameters);
    }

    [Fact]
    public void Given_Ids_When_BuildDelete_Then_DeletesWithIdArray()
    {
        // Arrange
        var builder = PostgreSqlStatementsBuilder.Create(BuildModel());
        long[] ids = [10, 11];

        // Act
        var statement = Assert.Single(builder.BuildDelete(ids));

        // Assert
        Assert.Equal("""DELETE FROM "OutboxMessage" WHERE "Id" = ANY({0})""", statement.Sql);
        Assert.Equal(new object[] { ids }, statement.Parameters);
    }

    [Fact]
    public void Given_ManyIds_When_BuildMarkAsProcessed_Then_SingleStatement()
    {
        // Arrange
        var builder = PostgreSqlStatementsBuilder.Create(BuildModel());
        var ids = Enumerable.Range(1, 5000).Select(i => (long)i).ToArray();

        // Act
        var statement = Assert.Single(builder.BuildMarkAsProcessed(ids, ProcessedAtUtc));

        // Assert
        Assert.Equal(new object[] { ProcessedAtUtc, ids }, statement.Parameters);
    }

    [Fact]
    public void Given_ModelWithoutOutbox_When_Create_Then_Throws()
    {
        // Arrange
        var model = BuildModel(_ => { });

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => PostgreSqlStatementsBuilder.Create(model));
        Assert.Equal("Entity type not found: OutboxMessage", exception.Message);
    }

    private static IModel BuildModel(Action<ModelBuilder>? configure = null)
        => TestModel.Build(SqlDialect.PostgreSql, configure);
}
