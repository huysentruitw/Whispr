using System.Text;
using Microsoft.Extensions.Logging;

namespace Whispr.AzureServiceBus.Transport;

/// <inheritdoc />
internal sealed partial class ServiceBusTransport
{
    public async ValueTask StartListener(
        string queueName,
        string[] topicNames,
        Func<SerializedEnvelope, CancellationToken, ValueTask> messageCallback,
        CancellationToken cancellationToken = default)
    {
        var subscriptionName = subscriptionNamingConvention.Format(queueName);
        await entityManager.CreateQueueIfNotExists(queueName, cancellationToken);
        foreach (var topicName in topicNames)
        {
            await entityManager.CreateTopicIfNotExists(topicName, cancellationToken);
            await entityManager.CreateSubscriptionIfNotExists(subscriptionName, topicName, queueName, cancellationToken);
        }

        var processor = processorFactory.GetOrCreateProcessor(queueName, options.QueueConcurrencyLimit);

        processor.ProcessMessageAsync += args => ProcessMessage(args, messageCallback);
        processor.ProcessErrorAsync += ProcessError;

        await processor.StartProcessingAsync(cancellationToken);
    }

    private async Task ProcessMessage(
        ProcessMessageEventArgs args,
        Func<SerializedEnvelope, CancellationToken, ValueTask> messageCallback)
    {
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
        await args.CompleteMessageAsync(args.Message, CancellationToken.None);
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

    public ValueTask StopListeners(CancellationToken cancellationToken = default)
        => processorFactory.StopAllProcessors(cancellationToken);
}
