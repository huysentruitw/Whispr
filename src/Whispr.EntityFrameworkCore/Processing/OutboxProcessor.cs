namespace Whispr.EntityFrameworkCore.Processing;

internal sealed class OutboxProcessor<TDbContext>(
    OutboxProcessorTrigger<TDbContext> trigger,
    IMessageSender messageSender,
    IOptions<OutboxOptions> options,
    IServiceProvider serviceProvider,
    IDiagnosticEventListener diagnosticEventListener,
    ILogger<OutboxProcessor<TDbContext>> logger) : BackgroundService
    where TDbContext : DbContext
{
    private readonly TimeSpan _queryDelay = options.Value.QueryDelay;
    private readonly TimeSpan _idleQueryDelay = options.Value.IdleQueryDelay;
    private readonly int _maxMessageBatchSize = options.Value.MaxMessageBatchSize;
    private readonly bool _messageRetentionEnabled = options.Value.EnableMessageRetention;
    private string? _sqlStatement = null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await trigger.Wait(_idleQueryDelay, stoppingToken);

            try
            {
                while (await SendOutboxMessages(stoppingToken) > 0)
                    await Task.Delay(_queryDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation exceptions
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing outbox messages");
            }
        }
    }

    private async ValueTask<int> SendOutboxMessages(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        _sqlStatement ??= GetOutboxSqlStatement<OutboxMessage>(dbContext, maxMessageBatchSize: _maxMessageBatchSize);
        var outboxMessages = await dbContext.Set<OutboxMessage>()
            .FromSqlRaw(_sqlStatement)
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        if (outboxMessages.Length == 0)
            return 0;

        var processedMessageIds = new List<long>();

        if (outboxMessages.Length == 1)
        {
            var messageId = await TrySendMessage(outboxMessages[0]);
            if (messageId is not null)
                processedMessageIds.Add(messageId.Value);
        }
        else
        {
            var sendTasks = outboxMessages.Select(TrySendMessage).ToArray();
            var results = await Task.WhenAll(sendTasks);
            processedMessageIds.AddRange(results.Where(x => x.HasValue).Select(x => x!.Value));
        }

        if (_messageRetentionEnabled)
        {
            var processedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.Set<OutboxMessage>()
                .Where(x => processedMessageIds.Contains(x.Id))
                .ExecuteUpdateAsync(x => x.SetProperty(m => m.ProcessedAtUtc, processedAtUtc), CancellationToken.None);
        }
        else
        {
            await dbContext.Set<OutboxMessage>()
                .Where(x => processedMessageIds.Contains(x.Id))
                .ExecuteDeleteAsync(CancellationToken.None);
        }

        return processedMessageIds.Count;
    }

    private async Task<long?> TrySendMessage(OutboxMessage outboxMessage)
    {
        try
        {
            using var _ = diagnosticEventListener.ProcessOutboxMessage(outboxMessage);
            var envelope = CreateSerializedEnvelope(outboxMessage);
            await messageSender.Send(outboxMessage.DestinationTopicName, envelope, CancellationToken.None);
            return outboxMessage.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send message with ID {MessageId} (Outbox ID {Id})", outboxMessage.MessageId, outboxMessage.Id);
            return null;
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

    private static SerializedEnvelope CreateSerializedEnvelope(OutboxMessage outboxMessage)
    {
        return new SerializedEnvelope
        {
            Body = outboxMessage.Body,
            MessageType = outboxMessage.MessageType,
            MessageId = outboxMessage.MessageId,
            CorrelationId = outboxMessage.CorrelationId,
            DeferredUntil = outboxMessage.DeferredUntil,
        };
    }
}
