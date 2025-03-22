using Whispr.EntityFrameworkCore.Entities;
using Whispr.EntityFrameworkCore.Processing;
using Whispr.Outbox;

namespace Whispr.EntityFrameworkCore;

internal sealed class Outbox<TDbContext>(TDbContext dbContext, OutboxProcessorTrigger<TDbContext> trigger) : IOutbox
    where TDbContext : DbContext
{
    public async ValueTask Add(string topicName, SerializedEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var outboxMessage = new OutboxMessage
        {
            Body = envelope.Body,
            MessageType = envelope.MessageType,
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            DeferredUntil = envelope.DeferredUntil,
            DestinationTopicName = topicName,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        await dbContext.Set<OutboxMessage>()
            .AddAsync(outboxMessage, cancellationToken);
    }

    public void Dispose()
    {
        // When this instance is disposed, everything should be saved to the database,
        // so the trigger should be released to allow the outbox processor to run.
        trigger.Release();
    }

    public ValueTask DisposeAsync()
    {
        // When this instance is disposed, everything should be saved to the database,
        // so the trigger should be released to allow the outbox processor to run.
        trigger.Release();
        return ValueTask.CompletedTask;
    }
}
