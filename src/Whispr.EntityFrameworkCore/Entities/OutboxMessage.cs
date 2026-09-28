using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Whispr.EntityFrameworkCore.Entities;

/// <summary>
/// Represents an outbox message.
/// </summary>
public sealed record OutboxMessage
{
    internal const int LastErrorMaxLength = 2000;

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
    public required string? CorrelationId { get; init; }

    /// <summary>
    /// The W3C trace parent ID.
    /// </summary>
    public required string? TraceParent { get; init; }

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

    /// <summary>
    /// The number of failed send attempts.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// The earliest date and time of the next send attempt in UTC. If <see langword="null"/>, the message is sent as soon as possible.
    /// </summary>
    public DateTimeOffset? NextAttemptAtUtc { get; set; }

    /// <summary>
    /// The date and time the outbox message was parked in UTC, after reaching <see cref="OutboxOptions.MaxSendAttempts"/>.
    /// A parked message is no longer retried, until this value is reset.
    /// </summary>
    public DateTimeOffset? ParkedAtUtc { get; set; }

    /// <summary>
    /// The error of the last failed send attempt.
    /// </summary>
    public string? LastError { get; set; }
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
            .HasMaxLength(500);

        builder.Property(x => x.MessageId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(50);

        builder.Property(x => x.TraceParent)
            .HasMaxLength(60); // W3C trace parent ID is 55 characters long

        builder.Property(x => x.DeferredUntil);

        builder.Property(x => x.DestinationTopicName)
            .IsRequired()
            .HasMaxLength(260); // Max length of an Azure Service Bus topic name

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.ProcessedAtUtc);

        builder.Property(x => x.AttemptCount)
            .IsRequired();

        builder.Property(x => x.NextAttemptAtUtc);

        builder.Property(x => x.ParkedAtUtc);

        builder.Property(x => x.LastError)
            .HasMaxLength(OutboxMessage.LastErrorMaxLength);

        // Used by the cleanup of processed messages
        builder.HasIndex(x => new { x.ProcessedAtUtc, x.CreatedAtUtc });

        // Only contains pending messages, so it stays small regardless of the number of processed or parked messages
        builder.HasIndex(x => x.CreatedAtUtc)
            .HasFilter("[ProcessedAtUtc] IS NULL AND [ParkedAtUtc] IS NULL");
    }
}
