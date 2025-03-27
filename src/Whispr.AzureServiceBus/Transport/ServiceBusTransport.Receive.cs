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

        var processor = processorFactory.GetOrCreateProcessor(queueName);

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

            throw new InvalidOperationException($"Failed to process message with correlation ID: {args.Message.CorrelationId}", ex);
        }

        await args.CompleteMessageAsync(args.Message, cancellationToken);
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
