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

        var processor = processorFactory.GetOrCreateProcessor(queueName, options.Value.QueueConcurrencyLimit);

        processor.ProcessMessageAsync += args => ProcessMessage(args, messageCallback, cancellationToken);
        processor.ProcessErrorAsync += args => ProcessError(args, cancellationToken);

        await processor.StartProcessingAsync(cancellationToken);
    }

    private async Task ProcessMessage(
        ProcessMessageEventArgs args,
        Func<SerializedEnvelope, CancellationToken, ValueTask> messageCallback,
        CancellationToken cancellationToken)
    {
        var messageType = args.Message.ApplicationProperties[MessageTypePropertyName]?.ToString();
        if (messageType is null)
        {
            await args.DeadLetterMessageAsync(
                args.Message,
                deadLetterReason: "Missing message type",
                deadLetterErrorDescription: "The message type is missing from the application properties.",
                cancellationToken: cancellationToken);

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
            await messageCallback(serializedEnvelope, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to process message with ID {MessageId} and correlation ID {CorrelationId}",
                args.Message.MessageId,
                args.Message.CorrelationId);

            // If the message is abandoned, it will be made available for reprocessing immediately.
            var exceptionDetails = GetExceptionDetails(ex);
            await args.AbandonMessageAsync(args.Message, exceptionDetails, cancellationToken);

            return;
        }

        await args.CompleteMessageAsync(args.Message, cancellationToken);
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

    private Task ProcessError(ProcessErrorEventArgs args, CancellationToken cancellationToken)
    {
        logger.LogError(
            args.Exception,
            "Error processing message from queue {QueueName}: {ErrorMessage}",
            args.FullyQualifiedNamespace,
            args.Exception.Message);

        return Task.CompletedTask;
    }
}
