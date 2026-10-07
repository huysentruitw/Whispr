using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Whispr.EntityFrameworkCore.Entities;
using Whispr.EntityFrameworkCore.Processing.SqlStatementBuilders;

namespace Whispr.EntityFrameworkCore.Tests.Processing.SqlStatementBuilders;

public sealed class MsSqlStatementsBuilderTests
{
    private const int MaxIdsPerStatement = 1024;

    private static readonly DateTimeOffset NowUtc = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private static readonly DateTimeOffset ProcessedAtUtc = new(2026, 1, 2, 3, 4, 6, TimeSpan.Zero);

    [Fact]
    public void Given_DefaultModel_When_BuildSelectBatch_Then_ReturnsLockingSelect()
    {
        // Arrange
        var builder = MsSqlStatementsBuilder.Create(BuildModel());

        // Act
        var statement = builder.BuildSelectBatch(maxMessageBatchSize: 50, NowUtc);

        // Assert
        Assert.Equal(
            """
            SELECT TOP 50 * FROM [OutboxMessage] WITH (UPDLOCK, ROWLOCK, READPAST)
            WHERE [ProcessedAtUtc] IS NULL AND [ParkedAtUtc] IS NULL AND ([NextAttemptAtUtc] IS NULL OR [NextAttemptAtUtc] <= {0})
            ORDER BY [CreatedAtUtc]
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
            modelBuilder.AddOutboxMessageEntity(tableName: "Outbox", schemaName: "messaging");
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.Property(x => x.CreatedAtUtc).HasColumnName("Created");
                entity.Property(x => x.ProcessedAtUtc).HasColumnName("Processed");
                entity.Property(x => x.ParkedAtUtc).HasColumnName("Parked");
                entity.Property(x => x.NextAttemptAtUtc).HasColumnName("NextAttempt");
            });
        });
        var builder = MsSqlStatementsBuilder.Create(model);

        // Act
        var statement = builder.BuildSelectBatch(maxMessageBatchSize: 50, NowUtc);

        // Assert
        Assert.Equal(
            """
            SELECT TOP 50 * FROM [messaging].[Outbox] WITH (UPDLOCK, ROWLOCK, READPAST)
            WHERE [Processed] IS NULL AND [Parked] IS NULL AND ([NextAttempt] IS NULL OR [NextAttempt] <= {0})
            ORDER BY [Created]
            """,
            statement.Sql);
    }

    [Fact]
    public void Given_NameWithClosingBracket_When_BuildSelectBatch_Then_NameIsEscaped()
    {
        // Arrange
        var model = BuildModel(modelBuilder => modelBuilder.AddOutboxMessageEntity(tableName: "Out]box"));
        var builder = MsSqlStatementsBuilder.Create(model);

        // Act
        var statement = builder.BuildSelectBatch(maxMessageBatchSize: 50, NowUtc);

        // Assert
        Assert.StartsWith("SELECT TOP 50 * FROM [Out]]box] WITH", statement.Sql);
    }

    [Fact]
    public void Given_Ids_When_BuildMarkAsProcessed_Then_UpdatesWithForceSeek()
    {
        // Arrange
        var model = BuildModel(modelBuilder =>
        {
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.Property(x => x.Id).HasColumnName("OutboxMessageId");
                entity.Property(x => x.ProcessedAtUtc).HasColumnName("Processed");
            });
        });
        var builder = MsSqlStatementsBuilder.Create(model);

        // Act
        var statement = Assert.Single(builder.BuildMarkAsProcessed([10, 11], ProcessedAtUtc));

        // Assert
        Assert.Equal(
            "UPDATE o SET [Processed] = {0} FROM [OutboxMessage] AS o WITH (FORCESEEK) WHERE o.[OutboxMessageId] IN ({1}, {2})",
            statement.Sql);
        Assert.Equal(new object[] { ProcessedAtUtc, 10L, 11L }, statement.Parameters);
    }

    [Fact]
    public void Given_Ids_When_BuildDelete_Then_DeletesWithForceSeek()
    {
        // Arrange
        var builder = MsSqlStatementsBuilder.Create(BuildModel());

        // Act
        var statement = Assert.Single(builder.BuildDelete([10, 11]));

        // Assert
        Assert.Equal(
            "DELETE o FROM [OutboxMessage] AS o WITH (FORCESEEK) WHERE o.[Id] IN ({0}, {1})",
            statement.Sql);
        Assert.Equal(new object[] { 10L, 11L }, statement.Parameters);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 4)]
    [InlineData(5, 8)]
    [InlineData(1000, 1024)]
    public void Given_IdCount_When_BuildDelete_Then_IdsPaddedWithLastIdToPowerOfTwo(int idCount, int expectedPaddedIdCount)
    {
        // Arrange
        var builder = MsSqlStatementsBuilder.Create(BuildModel());
        var ids = Enumerable.Range(1, idCount).Select(i => (long)i).ToArray();

        // Act
        var statement = Assert.Single(builder.BuildDelete(ids));

        // Assert
        Assert.Equal(expectedPaddedIdCount, statement.Parameters.Length);
        Assert.Equal(ids.Cast<object>(), statement.Parameters.Take(idCount));
        Assert.All(statement.Parameters.Skip(idCount), id => Assert.Equal((long)idCount, id));
        Assert.EndsWith($"{{{expectedPaddedIdCount - 1}}})", statement.Sql);
    }

    [Fact]
    public void Given_MoreIdsThanFitInOneStatement_When_BuildMarkAsProcessed_Then_IdsChunked()
    {
        // Arrange
        var builder = MsSqlStatementsBuilder.Create(BuildModel());
        var ids = Enumerable.Range(1, MaxIdsPerStatement + 3).Select(i => (long)i).ToArray();

        // Act
        var result = builder.BuildMarkAsProcessed(ids, ProcessedAtUtc).ToArray();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal(1 + MaxIdsPerStatement, result[0].Parameters.Length);
        Assert.Equal(new object[] { ProcessedAtUtc, 1025L, 1026L, 1027L, 1027L }, result[1].Parameters);
    }

    [Fact]
    public void Given_ModelWithoutOutbox_When_Create_Then_Throws()
    {
        // Arrange
        var model = BuildModel(_ => { });

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => MsSqlStatementsBuilder.Create(model));
        Assert.Equal("Entity type not found: OutboxMessage", exception.Message);
    }

    private static IModel BuildModel(Action<ModelBuilder>? configure = null)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer("Server=unused")
            .ReplaceService<IModelCacheKeyFactory, UncachedModelCacheKeyFactory>()
            .Options;

        using var context = new TestDbContext(options, configure ?? (modelBuilder => modelBuilder.AddOutboxMessageEntity()));
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
