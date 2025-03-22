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
    /// The message body.
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// The message type.
    /// </summary>
    public required string MessageType { get; init; }

    /// <summary>
    /// The message ID.
    /// </summary>
    public required string MessageId { get; init; }

    /// <summary>
    /// The correlation ID.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// The deferred until date and time. If <see langword="null"/>, the message is not deferred and will be processed immediately.
    /// </summary>
    public required DateTimeOffset? DeferredUntil { get; init; }

    /// <summary>
    /// The destination topic name.
    /// </summary>
    public required string DestinationTopicName { get; init; }

    /// <summary>
    /// The date and time the outbox message was created in UTC.
    /// </summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// The date and time the outbox message was processed in UTC.
    /// </summary>
    public DateTimeOffset? ProcessedAtUtc { get; set; }
}

internal sealed class OutboxMessageEntityTypeConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Body)
            .IsRequired();

        builder.Property(x => x.MessageType)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.MessageId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.CorrelationId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DeferredUntil);

        builder.Property(x => x.DestinationTopicName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.ProcessedAtUtc);

        builder.HasIndex(x => new { x.ProcessedAtUtc, x.CreatedAtUtc });
    }
}
