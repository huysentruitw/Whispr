using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pigeon.EntityFrameworkCore.Entities;

/// <summary>
/// Represents an outbox message.
/// </summary>
public sealed record OutboxMessage
{
    /// <summary>
    /// The outbox message ID.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// The serialized envelope.
    /// </summary>
    public required SerializedEnvelope Envelope { get; init; }

    /// <summary>
    /// The destination topic name.
    /// </summary>
    public required string DestinationTopicName { get; init; }

    /// <summary>
    /// The date and time the outbox message was created in UTC.
    /// </summary>
    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// The date and time the outbox message was processed in UTC.
    /// </summary>
    public DateTime? ProcessedAtUtc { get; set; }
}

internal sealed class OutboxMessageEntityTypeConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.OwnsOne(x => x.Envelope, envelope =>
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

        builder.Property(x => x.DestinationTopicName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.ProcessedAtUtc);

        builder.HasIndex(x => new { x.ProcessedAtUtc, x.CreatedAtUtc });
    }
}
