using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pigeon.EntityFrameworkCore.Entities;
using Pigeon.Transport;

namespace Pigeon.EntityFrameworkCore.Processing;

internal sealed class OutboxProcessor<TDbContext>(
    OutboxProcessorTrigger<TDbContext> trigger,
    ITransport transport,
    IOptions<OutboxOptions> options,
    IServiceProvider serviceProvider) : BackgroundService
    where TDbContext : DbContext
{
    private readonly TimeSpan _queryDelay = options.Value.QueryDelay;
    private readonly int _maxMessageBatchSize = options.Value.MaxMessageBatchSize;
    private readonly bool _messageRetentionEnabled = options.Value.EnableMessageRetention;
    private string? _sqlStatement = null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await trigger.Wait(_queryDelay, stoppingToken);

            await SendOutboxMessages(stoppingToken);
        }
    }

    private async ValueTask SendOutboxMessages(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        _sqlStatement ??= GetOutboxSqlStatement<OutboxMessage>(dbContext, maxMessageBatchSize: _maxMessageBatchSize);
        var outboxMessages = await dbContext.Set<OutboxMessage>()
            .FromSqlRaw(_sqlStatement)
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        if (outboxMessages.Length == 0)
            return;

        foreach (var outboxMessage in outboxMessages)
        {
            await transport.Send(outboxMessage.DestinationTopicName, outboxMessage.Envelope, CancellationToken.None);
        }

        if (_messageRetentionEnabled)
        {
            var processedAtUtc = DateTime.UtcNow;
            await dbContext.Set<OutboxMessage>()
                .Where(x => outboxMessages.Select(m => m.Id).Contains(x.Id))
                .ExecuteUpdateAsync(x => x.SetProperty(m => m.ProcessedAtUtc, processedAtUtc), CancellationToken.None);
        }
        else
        {
            await dbContext.Set<OutboxMessage>()
                .Where(x => outboxMessages.Select(m => m.Id).Contains(x.Id))
                .ExecuteDeleteAsync(CancellationToken.None);
        }
    }

    private static string GetOutboxSqlStatement<TEntity>(DbContext context, int maxMessageBatchSize)
    {
        var tableName = GetTableName<TEntity>(context);
        return $"SELECT TOP {maxMessageBatchSize} * FROM {tableName} WITH (UPDLOCK, READPAST) WHERE [ProcessedAtUtc] IS NULL ORDER BY [CreatedAtUtc]";
    }

    private static string GetTableName<TEntity>(DbContext context)
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity type not found: {typeof(TEntity).Name}");

        var schema = entityType.GetSchema();
        var tableName = entityType.GetTableName();

        if (string.IsNullOrWhiteSpace(tableName))
            throw new InvalidOperationException($"Table name not found for entity type: {typeof(TEntity).Name}");

        return schema is null ? $"[{tableName}]" : $"[{schema}].[{tableName}]";
    }
}
