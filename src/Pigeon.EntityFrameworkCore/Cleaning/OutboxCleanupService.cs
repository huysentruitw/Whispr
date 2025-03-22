using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pigeon.EntityFrameworkCore.Entities;

namespace Pigeon.EntityFrameworkCore.Cleaning;

internal sealed class OutboxCleanupService<TDbContext>(
    IOptions<OutboxOptions> options,
    ILogger<OutboxCleanupService<TDbContext>> logger,
    IServiceProvider serviceProvider) : BackgroundService
    where TDbContext : DbContext
{
    private readonly bool _messageRetentionEnabled = options.Value.EnableMessageRetention;
    private readonly TimeSpan _cleanupDelay = options.Value.ProcessedMessageCleanupDelay;
    private readonly TimeSpan _retentionPeriod = options.Value.ProcessedMessageRetentionPeriod;
    private readonly int _batchSize = options.Value.ProcessedMessageCleanupBatchSize;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // When message retention is disabled, we don't need to run the cleanup service as
        // the outbox processor will delete the messages after they are sent.
        if (!_messageRetentionEnabled)
        {
            logger.LogInformation("Outbox message retention is disabled, cleanup service will not run");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_cleanupDelay, stoppingToken);

            await CleanupOutboxMessages(stoppingToken);
        }
    }

    private async ValueTask CleanupOutboxMessages(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        while (true)
        {
            var expiredUtc = DateTimeOffset.UtcNow - _retentionPeriod;
            var rowsDeleted = await dbContext.Set<OutboxMessage>()
                .Take(_batchSize)
                .OrderBy(x => x.ProcessedAtUtc)
                .Where(x => x.ProcessedAtUtc < expiredUtc)
                .ExecuteDeleteAsync(cancellationToken);

            if (rowsDeleted < _batchSize)
                break;

            logger.LogInformation("Deleted {RowsDeleted} outbox messages older than {RetentionPeriod}", rowsDeleted, _retentionPeriod);

            // Wait a bit before checking again to avoid hammering the database.
            await Task.Delay(100, cancellationToken);
        }
    }
}
