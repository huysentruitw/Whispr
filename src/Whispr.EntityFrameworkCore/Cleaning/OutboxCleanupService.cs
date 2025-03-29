namespace Whispr.EntityFrameworkCore.Cleaning;

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

            try
            {
                await CleanupOutboxMessages(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation exceptions
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while cleaning up outbox messages");
            }
        }
    }

    private async ValueTask CleanupOutboxMessages(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        while (!cancellationToken.IsCancellationRequested)
        {
            var expiredUtc = DateTimeOffset.UtcNow - _retentionPeriod;
            var rowsDeleted = await dbContext.Set<OutboxMessage>()
                .OrderBy(x => x.ProcessedAtUtc)
                .Where(x => x.ProcessedAtUtc < expiredUtc)
                .Take(_batchSize)
                .ExecuteDeleteAsync(cancellationToken);

            if (rowsDeleted != 0)
                logger.LogInformation("Deleted {RowsDeleted} outbox messages older than {RetentionPeriod}", rowsDeleted, _retentionPeriod);

            if (rowsDeleted < _batchSize)
                break;

            // Wait a bit before checking again to avoid hammering the database.
            await Task.Delay(100, cancellationToken);
        }
    }
}
