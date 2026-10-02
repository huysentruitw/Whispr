using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Whispr.AzureServiceBus.Transport;

/// <inheritdoc />
internal sealed partial class ServiceBusTransport : IAsyncDisposable
{
    // Lock renewal stops when the callback returns, so only complete in the background while the lock outlives a retried completion
    private static readonly TimeSpan MinimumLockForBackgroundCompletion = TimeSpan.FromSeconds(60);

    private readonly ConcurrentDictionary<string, PendingCompletions> _pendingCompletions = new();

    public async ValueTask StartListener(
        string queueName,
        string[] topicNames,
        Func<SerializedEnvelope, CancellationToken, ValueTask> messageCallback,
        CancellationToken cancellationToken = default)
    {
        if (options.CompleteMessagesInBackground)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxPendingCompletions, nameof(AzureServiceBusOptions.MaxPendingCompletions));

        var subscriptionName = subscriptionNamingConvention.Format(queueName);
        await entityManager.CreateQueueIfNotExists(queueName, cancellationToken);
        foreach (var topicName in topicNames)
        {
            await entityManager.CreateTopicIfNotExists(topicName, cancellationToken);
            await entityManager.CreateSubscriptionIfNotExists(subscriptionName, topicName, queueName, cancellationToken);
        }

        var pendingCompletions = options.CompleteMessagesInBackground
            ? _pendingCompletions.GetOrAdd(queueName, _ => new PendingCompletions(options.MaxPendingCompletions))
            : null;

        // The broker withholds the next delivery while earlier ones are unsettled, so pending completions need prefetch room
        var prefetchCount = pendingCompletions is null
            ? options.QueueConcurrencyLimit
            : options.QueueConcurrencyLimit + options.MaxPendingCompletions;

        var processor = processorFactory.GetOrCreateProcessor(queueName, options.QueueConcurrencyLimit, prefetchCount);

        processor.ProcessMessageAsync += args => ProcessMessage(args, messageCallback, pendingCompletions);
        processor.ProcessErrorAsync += ProcessError;

        await processor.StartProcessingAsync(cancellationToken);
    }

    private async Task ProcessMessage(
        ProcessMessageEventArgs args,
        Func<SerializedEnvelope, CancellationToken, ValueTask> messageCallback,
        PendingCompletions? pendingCompletions)
    {
        // Nothing renews the lock of a prefetched message, and once it expired the broker already handed the message to someone else
        if (pendingCompletions is not null && args.Message.LockedUntil <= DateTimeOffset.UtcNow)
        {
            await AbandonQuietly(args);
            return;
        }

        var messageType = args.Message.ApplicationProperties.TryGetValue(MessageTypePropertyName, out var messageTypeProperty)
            ? messageTypeProperty?.ToString()
            : null;

        if (messageType is null)
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                deadLetterReason: "Missing message type",
                deadLetterErrorDescription: "The message type is missing from the application properties.",
                cancellationToken: CancellationToken.None);

            return;
        }

        var messageBody = Encoding.UTF8.GetString(args.Message.Body);

        var serializedEnvelope = new SerializedEnvelope
        {
            Body = messageBody,
            MessageType = messageType,
            MessageId = args.Message.MessageId,
            CorrelationId = args.Message.CorrelationId,
            DeferredUntil = args.Message.ScheduledEnqueueTime != default ? args.Message.ScheduledEnqueueTime : null,
        };

        try
        {
            // The args cancellation token is signaled when the processor is stopping
            await messageCallback(serializedEnvelope, args.CancellationToken);
        }
        catch (UnsupportedMessageTypeException ex)
        {
            logger.LogWarning(
                ex,
                "Dead-lettering message with ID {MessageId} from queue {QueueName}: unsupported message type {MessageType}",
                args.Message.MessageId,
                args.EntityPath,
                messageType);

            // Retrying won't help, so dead-letter immediately instead of exhausting the max delivery count.
            await args.DeadLetterMessageAsync(
                args.Message,
                deadLetterReason: "Unsupported message type",
                deadLetterErrorDescription: ex.Message,
                cancellationToken: CancellationToken.None);

            return;
        }
        catch (MessageDeserializationException ex)
        {
            logger.LogWarning(
                ex,
                "Dead-lettering message with ID {MessageId} from queue {QueueName}: failed to deserialize message type {MessageType}",
                args.Message.MessageId,
                args.EntityPath,
                messageType);

            // The body won't change between deliveries, so retrying won't help either.
            await args.DeadLetterMessageAsync(
                args.Message,
                deadLetterReason: "Deserialization failed",
                deadLetterErrorDescription: ex.InnerException?.Message ?? ex.Message,
                cancellationToken: CancellationToken.None);

            return;
        }
        catch (Exception ex)
        {
            var retryDelay = GetRetryDelay(args.Message.DeliveryCount, options.RetryBackoffBase, options.RetryBackoffMax);

            logger.LogError(
                ex,
                "Failed to process message with ID {MessageId} and correlation ID {CorrelationId} (delivery {DeliveryCount}), abandoning in {RetryDelay}",
                args.Message.MessageId,
                args.Message.CorrelationId,
                args.Message.DeliveryCount,
                retryDelay);

            // An abandoned message is made available for reprocessing immediately, so wait before abandoning it to
            // survive short outages of external dependencies. The processor keeps renewing the lock in the meantime.
            await DelayRetry(retryDelay, args.CancellationToken);

            var exceptionDetails = GetExceptionDetails(ex);
            await args.AbandonMessageAsync(args.Message, exceptionDetails, CancellationToken.None);

            return;
        }

        // Settle without cancellation, so a successfully handled message isn't reprocessed when the processor is stopping
        if (pendingCompletions is null || args.Message.LockedUntil - DateTimeOffset.UtcNow < MinimumLockForBackgroundCompletion)
        {
            await args.CompleteMessageAsync(args.Message, CancellationToken.None);
            return;
        }

        await pendingCompletions.Track(() => CompleteInBackground(args));
    }

    private async Task CompleteInBackground(ProcessMessageEventArgs args)
    {
        try
        {
            await args.CompleteMessageAsync(args.Message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to complete message with ID {MessageId} from queue {QueueName}",
                args.Message.MessageId,
                args.EntityPath);

            // Like the processor does when an awaited completion fails, so the message is redelivered now instead of when its lock expires
            if (ex is not ServiceBusException { Reason: ServiceBusFailureReason.MessageLockLost })
                await AbandonQuietly(args);
        }
    }

    private static async Task AbandonQuietly(ProcessMessageEventArgs args)
    {
        try
        {
            await args.AbandonMessageAsync(args.Message, cancellationToken: CancellationToken.None);
        }
        catch (ServiceBusException)
        {
            // The lock is gone already, which releases the message just the same
        }
    }

    internal static TimeSpan GetRetryDelay(int deliveryCount, TimeSpan retryBackoffBase, TimeSpan retryBackoffMax)
    {
        if (retryBackoffBase <= TimeSpan.Zero)
            return TimeSpan.Zero;

        // Cap the exponent to prevent overflow, the result is capped by the max backoff anyway
        var exponent = Math.Clamp(deliveryCount - 1, 0, 16);
        var delayTicks = retryBackoffBase.Ticks << exponent;
        return delayTicks >= retryBackoffMax.Ticks ? retryBackoffMax : TimeSpan.FromTicks(delayTicks);
    }

    private static async Task DelayRetry(TimeSpan retryDelay, CancellationToken cancellationToken)
    {
        if (retryDelay <= TimeSpan.Zero)
            return;

        try
        {
            await Task.Delay(retryDelay, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // The processor is stopping, abandon right away so the message can be picked up by another instance
        }
    }

    private static IDictionary<string, object> GetExceptionDetails(Exception exception)
    {
        return new Dictionary<string, object>
        {
            { "ExceptionType", exception.GetType().Name },
            { "ExceptionMessage", exception.Message },
            { "StackTrace", GetStackTrace(exception) },
        };

        string GetStackTrace(Exception ex)
        {
            // Official max is 32KB, so 16K-unicode chars (see https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quotas)
            const int maxStackTraceLength = 4_000;

            if (string.IsNullOrWhiteSpace(ex.StackTrace))
                return string.Empty;

            return ex.StackTrace.Length > maxStackTraceLength
                ? ex.StackTrace[..maxStackTraceLength]
                : ex.StackTrace;
        }
    }

    private Task ProcessError(ProcessErrorEventArgs args)
    {
        logger.LogError(
            args.Exception,
            "Error processing message from queue {QueueName}: {ErrorMessage}",
            args.EntityPath,
            args.Exception.Message);

        return Task.CompletedTask;
    }

    public async ValueTask StopListeners(CancellationToken cancellationToken = default)
    {
        await processorFactory.StopAllProcessors(cancellationToken);

        // Stopping a processor keeps its receiver open, so completions still in flight can land before it is disposed
        foreach (var pendingCompletions in _pendingCompletions.Values)
            await pendingCompletions.Drain(cancellationToken);
    }

    // Disposed before the processor factory it depends on, and covers a drain that StopListeners cut short
    public async ValueTask DisposeAsync()
    {
        await processorFactory.StopAllProcessors(CancellationToken.None);

        foreach (var pendingCompletions in _pendingCompletions.Values)
            await pendingCompletions.Drain(CancellationToken.None);
    }
}
