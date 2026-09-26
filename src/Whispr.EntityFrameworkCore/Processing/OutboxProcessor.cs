namespace Whispr.EntityFrameworkCore.Processing;

internal sealed class OutboxProcessor<TDbContext>(
    string busName,
    OutboxProcessorTrigger<TDbContext> trigger,
    IMessageSender messageSender,
    IServiceScopeFactory serviceScopeFactory,
    OutboxOptions options,
    IDiagnosticEventListener diagnosticEventListener,
    ILogger<OutboxProcessor<TDbContext>> logger) : BackgroundService
    where TDbContext : DbContext
{
    private readonly TimeSpan _queryDelay = options.QueryDelay;
    private readonly TimeSpan _idleQueryDelay = options.IdleQueryDelay;
    private readonly int _maxMessageBatchSize = options.MaxMessageBatchSize;
    private readonly bool _messageRetentionEnabled = options.EnableMessageRetention;
    private readonly int? _maxSendAttempts = options.MaxSendAttempts;
    private readonly TimeSpan _retryBackoffBase = options.RetryBackoffBase;
    private readonly TimeSpan _retryBackoffMax = options.RetryBackoffMax;
    private string? _sqlStatement;

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
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        _sqlStatement ??= GetOutboxSqlStatement<OutboxMessage>(dbContext, maxMessageBatchSize: _maxMessageBatchSize);

        // The execution strategy is required when the user configured retry on failure on the DbContext,
        // as user-initiated transactions are not allowed otherwise.
        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(
            ct => SendOutboxMessages(dbContext, _sqlStatement, ct).AsTask(),
            cancellationToken);
    }

    private async ValueTask<int> SendOutboxMessages(TDbContext dbContext, string sqlStatement, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var outboxMessages = await dbContext.Set<OutboxMessage>()
            .FromSqlRaw(sqlStatement, DateTimeOffset.UtcNow)
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        if (outboxMessages.Length == 0)
            return 0;

        var results = outboxMessages.Length == 1
            ? [await TrySendMessage(outboxMessages[0])]
            : await Task.WhenAll(outboxMessages.Select(TrySendMessage));

        var processedMessageIds = results
            .Where(x => x.Error is null)
            .Select(x => x.Message.Id)
            .ToArray();

        foreach (var failedResult in results.Where(x => x.Error is not null))
            await RegisterFailedAttempt(dbContext, failedResult.Message, failedResult.Error!);

        if (processedMessageIds.Length > 0)
            await MarkAsProcessed(dbContext, processedMessageIds);

        await transaction.CommitAsync(CancellationToken.None);

        return processedMessageIds.Length;
    }

    private async ValueTask MarkAsProcessed(TDbContext dbContext, long[] processedMessageIds)
    {
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
    }

    private async Task<SendResult> TrySendMessage(OutboxMessage outboxMessage)
    {
        try
        {
            using var _ = diagnosticEventListener.ProcessOutboxMessage(busName, outboxMessage);
            var envelope = CreateSerializedEnvelope(outboxMessage);
            await messageSender.Send(outboxMessage.DestinationTopicName, envelope, CancellationToken.None);
            return new SendResult(outboxMessage, Error: null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send message with ID {MessageId} (Outbox ID {Id})", outboxMessage.MessageId, outboxMessage.Id);
            return new SendResult(outboxMessage, Error: ex);
        }
    }

    private async ValueTask RegisterFailedAttempt(TDbContext dbContext, OutboxMessage outboxMessage, Exception error)
    {
        var attemptCount = outboxMessage.AttemptCount + 1;
        var nowUtc = DateTimeOffset.UtcNow;
        var nextAttemptAtUtc = nowUtc + GetRetryBackoff(attemptCount);
        var parkedAtUtc = attemptCount >= _maxSendAttempts ? nowUtc : (DateTimeOffset?)null;
        var lastError = Truncate($"{error.GetType().FullName}: {error.Message}", OutboxMessage.LastErrorMaxLength);

        await dbContext.Set<OutboxMessage>()
            .Where(x => x.Id == outboxMessage.Id)
            .ExecuteUpdateAsync(
                x => x
                    .SetProperty(m => m.AttemptCount, attemptCount)
                    .SetProperty(m => m.NextAttemptAtUtc, nextAttemptAtUtc)
                    .SetProperty(m => m.ParkedAtUtc, parkedAtUtc)
                    .SetProperty(m => m.LastError, lastError),
                CancellationToken.None);

        if (parkedAtUtc is not null)
        {
            logger.LogError(
                "Parked message with ID {MessageId} (Outbox ID {Id}) after {AttemptCount} failed send attempts",
                outboxMessage.MessageId,
                outboxMessage.Id,
                attemptCount);
        }
    }

    private TimeSpan GetRetryBackoff(int attemptCount)
    {
        // Cap the exponent to prevent overflow, the result is capped by the max backoff anyway
        var exponent = Math.Clamp(attemptCount - 1, 0, 16);
        var backoffTicks = _retryBackoffBase.Ticks << exponent;
        return backoffTicks >= _retryBackoffMax.Ticks ? _retryBackoffMax : TimeSpan.FromTicks(backoffTicks);
    }

    private static string Truncate(string value, int maxLength)
        => value.Length > maxLength ? value[..maxLength] : value;

    private static string GetOutboxSqlStatement<TEntity>(DbContext context, int maxMessageBatchSize)
    {
        var tableName = GetTableName<TEntity>(context);

        // {0} is the current UTC time, passed as parameter so the application clock is used for both scheduling and querying
        return $$"""
            SELECT TOP {{maxMessageBatchSize}} * FROM {{tableName}} WITH (UPDLOCK, ROWLOCK, READPAST)
            WHERE [ProcessedAtUtc] IS NULL AND [ParkedAtUtc] IS NULL AND ([NextAttemptAtUtc] IS NULL OR [NextAttemptAtUtc] <= {0})
            ORDER BY [CreatedAtUtc]
            """;
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

    private sealed record SendResult(OutboxMessage Message, Exception? Error);
}
