namespace Whispr.EntityFrameworkCore;

/// <summary>
/// Extension methods for the <see cref="ModelBuilder"/>.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Adds the outbox message entity to the model builder.
    /// </summary>
    /// <param name="modelBuilder">The <see cref="ModelBuilder"/>.</param>
    /// <param name="tableName">The table name of the outbox message entity.</param>
    /// <param name="schemaName">The schema name of the outbox message entity.</param>
    /// <returns>The <see cref="ModelBuilder"/>.</returns>
    public static ModelBuilder AddOutboxMessageEntity(
        this ModelBuilder modelBuilder,
        string tableName = "OutboxMessage",
        string? schemaName = null)
    {
        return modelBuilder
            .Entity<OutboxMessage>(entity => entity.ToTable(tableName, schemaName))
            .ApplyConfiguration(new OutboxMessageEntityTypeConfiguration());
    }
}
