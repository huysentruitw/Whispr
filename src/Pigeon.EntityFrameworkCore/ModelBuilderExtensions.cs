namespace Pigeon.EntityFrameworkCore;

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
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable(tableName, schemaName);

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            entity.OwnsOne(x => x.Envelope, envelope =>
            {
                envelope.Property(x => x.Body)
                    .IsRequired();

                envelope.Property(x => x.MessageType)
                    .IsRequired()
                    .HasMaxLength(250);

                envelope.Property(x => x.MessageId)
                    .IsRequired()
                    .HasMaxLength(50);

                envelope.Property(x => x.CorrelationId)
                    .IsRequired()
                    .HasMaxLength(50);

                envelope.Property(x => x.DeferredUntil);
            });

            entity.Property(x => x.DestinationTopicName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            entity.Property(x => x.ProcessedAtUtc);

            entity.HasIndex(x => new { x.ProcessedAtUtc, x.CreatedAtUtc });
        });

        return modelBuilder;
    }
}
