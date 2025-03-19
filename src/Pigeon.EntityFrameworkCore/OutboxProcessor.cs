using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pigeon.Transport;

namespace Pigeon.EntityFrameworkCore;

internal sealed class OutboxProcessor<TDbContext>(
    OutboxProcessorTrigger<TDbContext> trigger,
    ITransport transport,
    IServiceProvider serviceProvider) : BackgroundService
    where TDbContext : DbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await trigger.Wait(stoppingToken);

            await using var scope = serviceProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

            var outboxMessages = await dbContext.Set<OutboxMessage>()
                .AsTracking()
                .OrderBy(message => message.CreatedAtUtc)
                .Take(100)
                .ToListAsync(stoppingToken);

            foreach (var outboxMessage in outboxMessages)
            {
                await transport.Send(outboxMessage.DestinationTopicName, outboxMessage.Envelope, stoppingToken);
                outboxMessage.ProcessedAtUtc = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
