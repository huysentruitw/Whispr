using Pigeon.Filtering;

namespace Pigeon.EntityFrameworkCore;

internal sealed class OutboxSendFilter<TDbContext>(
    TDbContext dbContext,
    OutboxProcessorTrigger<TDbContext> trigger) : ISendFilter
    where TDbContext : DbContext
{
    public async ValueTask Send(
        string topicName,
        SerializedEnvelope envelope,
        Func<string, SerializedEnvelope, CancellationToken, ValueTask> next,
        CancellationToken cancellationToken)
    {
        var outboxMessage = new OutboxMessage
        {
            Envelope = envelope,
            DestinationTopicName = topicName,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await dbContext.Set<OutboxMessage>()
            .AddAsync(outboxMessage, cancellationToken);

        trigger.Release();
    }
}
