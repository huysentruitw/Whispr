using Pigeon.EntityFrameworkCore.Entities;
using Pigeon.EntityFrameworkCore.Processing;
using Pigeon.Outbox;

namespace Pigeon.EntityFrameworkCore;

internal sealed class Outbox<TDbContext>(TDbContext dbContext, OutboxProcessorTrigger<TDbContext> trigger) : IOutbox
    where TDbContext : DbContext
{
    public async ValueTask Add(string topicName, SerializedEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var outboxMessage = new OutboxMessage
        {
            Envelope = envelope,
            DestinationTopicName = topicName,
            CreatedAtUtc = DateTime.UtcNow,
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
