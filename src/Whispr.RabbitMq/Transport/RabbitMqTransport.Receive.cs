using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using Whispr.RabbitMq.Management;

namespace Whispr.RabbitMq.Transport;

/// <inheritdoc />
internal sealed partial class RabbitMqTransport
{
    private const string DeliveryCountHeaderName = "x-delivery-count";

    private readonly ConcurrentQueue<Listener> _listeners = new();

    public async ValueTask StartListener(
        string queueName,
        string[] topicNames,
        Func<SerializedEnvelope, CancellationToken, ValueTask> messageCallback,
        CancellationToken cancellationToken = default)
    {
        await topologyManager.DeclareQueue(queueName, topicNames, cancellationToken);

        var concurrencyLimit = (ushort)Math.Clamp(options.QueueConcurrencyLimit, 1, ushort.MaxValue);
        var connection = await connectionProvider.GetConnection(cancellationToken);
        var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: false,
                publisherConfirmationTrackingEnabled: false,
                consumerDispatchConcurrency: concurrencyLimit),
            cancellationToken);

        // Limit the unacknowledged messages to the ones being handled, so other instances can pick up the rest
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: concurrencyLimit, global: false, cancellationToken);

        var listener = new Listener(queueName, channel);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, args) => ProcessMessage(listener, args, messageCallback);

        await listener.Start(consumer, cancellationToken);
        _listeners.Enqueue(listener);
    }

    private async Task ProcessMessage(
        Listener listener,
        BasicDeliverEventArgs args,
        Func<SerializedEnvelope, CancellationToken, ValueTask> messageCallback)
    {
        // When the listener stopped already, the message is made available again when its channel closes
        if (!listener.TryEnter())
            return;

        try
        {
            await ProcessMessage(listener, args, messageCallback, args.BasicProperties.Headers);
        }
        catch (Exception ex)
        {
            // Settling fails when the channel was closed, e.g. by a connection failure. The message is redelivered then.
            logger.LogError(
                ex,
                "Failed to settle message with ID {MessageId} from queue {QueueName}",
                args.BasicProperties.MessageId,
                listener.QueueName);
        }
        finally
        {
            listener.Exit();
        }
    }

    private async Task ProcessMessage(
        Listener listener,
        BasicDeliverEventArgs args,
        Func<SerializedEnvelope, CancellationToken, ValueTask> messageCallback,
        IDictionary<string, object?>? headers)
    {
        var messageType = GetStringHeader(headers, MessageTypeHeaderName);

        if (messageType is null)
        {
            await DeadLetter(
                listener,
                args,
                deadLetterReason: "Missing message type",
                deadLetterErrorDescription: "The message type is missing from the headers.");

            return;
        }

        var serializedEnvelope = new SerializedEnvelope
        {
            Body = Encoding.UTF8.GetString(args.Body.Span),
            MessageType = messageType,
            MessageId = args.BasicProperties.MessageId ?? string.Empty,
            CorrelationId = args.BasicProperties.CorrelationId,
            DeferredUntil = null,
        };

        var deliveryCount = GetDeliveryCount(headers);

        try
        {
            // The stopping token is signaled when the listener is stopping
            await messageCallback(serializedEnvelope, listener.StoppingToken);
        }
        catch (UnsupportedMessageTypeException ex)
        {
            logger.LogWarning(
                ex,
                "Dead-lettering message with ID {MessageId} from queue {QueueName}: unsupported message type {MessageType}",
                serializedEnvelope.MessageId,
                listener.QueueName,
                messageType);

            // Retrying won't help, so dead-letter immediately instead of exhausting the max delivery count.
            await DeadLetter(listener, args, deadLetterReason: "Unsupported message type", deadLetterErrorDescription: ex.Message);
            return;
        }
        catch (MessageDeserializationException ex)
        {
            logger.LogWarning(
                ex,
                "Dead-lettering message with ID {MessageId} from queue {QueueName}: failed to deserialize message type {MessageType}",
                serializedEnvelope.MessageId,
                listener.QueueName,
                messageType);

            // The body won't change between deliveries, so retrying won't help either.
            await DeadLetter(
                listener,
                args,
                deadLetterReason: "Deserialization failed",
                deadLetterErrorDescription: ex.InnerException?.Message ?? ex.Message);

            return;
        }
        catch (Exception ex) when (deliveryCount >= options.MaxDeliveryCount && !listener.StoppingToken.IsCancellationRequested)
        {
            logger.LogError(
                ex,
                "Failed to process message with ID {MessageId} and correlation ID {CorrelationId} (delivery {DeliveryCount}), dead-lettering because the max delivery count is reached",
                serializedEnvelope.MessageId,
                serializedEnvelope.CorrelationId,
                deliveryCount);

            await DeadLetter(listener, args, deadLetterReason: "MaxDeliveryCountExceeded", deadLetterErrorDescription: ex.Message, ex);
            return;
        }
        catch (Exception ex)
        {
            var retryDelay = GetRetryDelay(deliveryCount, options.RetryBackoffBase, options.RetryBackoffMax);

            logger.LogError(
                ex,
                "Failed to process message with ID {MessageId} and correlation ID {CorrelationId} (delivery {DeliveryCount}), requeueing in {RetryDelay}",
                serializedEnvelope.MessageId,
                serializedEnvelope.CorrelationId,
                deliveryCount,
                retryDelay);

            // A requeued message is made available for reprocessing immediately, so wait before requeueing it to
            // survive short outages of external dependencies. The message stays unacknowledged in the meantime.
            await DelayRetry(retryDelay, listener.StoppingToken);

            await listener.Channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, CancellationToken.None);
            return;
        }

        // Settle without cancellation, so a successfully handled message isn't reprocessed when the listener is stopping
        await listener.Channel.BasicAckAsync(args.DeliveryTag, multiple: false, CancellationToken.None);
    }

    private async Task DeadLetter(
        Listener listener,
        BasicDeliverEventArgs args,
        string deadLetterReason,
        string deadLetterErrorDescription,
        Exception? exception = null)
    {
        var headers = new Dictionary<string, object?>(args.BasicProperties.Headers ?? new Dictionary<string, object?>())
        {
            ["DeadLetterReason"] = deadLetterReason,
            ["DeadLetterErrorDescription"] = deadLetterErrorDescription,
        };

        if (exception is not null)
        {
            headers["ExceptionType"] = exception.GetType().Name;
            headers["StackTrace"] = GetStackTrace(exception);
        }

        var properties = new BasicProperties(args.BasicProperties) { Headers = headers };
        var deadLetterQueueName = TopologyManager.GetDeadLetterQueueName(listener.QueueName);

        try
        {
            // Publish through the default exchange, which routes to the queue with the same name as the routing key
            await publishChannelPool.Use(
                channel => channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: deadLetterQueueName,
                    mandatory: false,
                    properties,
                    args.Body,
                    CancellationToken.None),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to dead-letter message with ID {MessageId} to queue {DeadLetterQueueName}, requeueing it",
                args.BasicProperties.MessageId,
                deadLetterQueueName);

            await listener.Channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, CancellationToken.None);
            return;
        }

        await listener.Channel.BasicAckAsync(args.DeliveryTag, multiple: false, CancellationToken.None);
    }

    internal static string? GetStringHeader(IDictionary<string, object?>? headers, string name)
    {
        if (headers is null || !headers.TryGetValue(name, out var value))
            return null;

        // String headers are received as UTF-8 encoded bytes
        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => value?.ToString(),
        };
    }

    internal static int GetDeliveryCount(IDictionary<string, object?>? headers)
    {
        // Quorum queues count the earlier, failed deliveries in this header, which is absent on the first delivery
        if (headers is null || !headers.TryGetValue(DeliveryCountHeaderName, out var value) || value is null)
            return 1;

        var previousDeliveries = Convert.ToInt64(value);
        return (int)Math.Clamp(previousDeliveries + 1, 1, int.MaxValue);
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
            // The listener is stopping, requeue right away so the message can be picked up by another instance
        }
    }

    private static string GetStackTrace(Exception exception)
    {
        // Headers count towards the max frame size of the broker, so keep them small
        const int maxStackTraceLength = 4_000;

        if (string.IsNullOrWhiteSpace(exception.StackTrace))
            return string.Empty;

        return exception.StackTrace.Length > maxStackTraceLength
            ? exception.StackTrace[..maxStackTraceLength]
            : exception.StackTrace;
    }

    public async ValueTask StopListeners(CancellationToken cancellationToken = default)
    {
        var listeners = new List<Listener>();
        while (_listeners.TryDequeue(out var listener))
            listeners.Add(listener);

        await Task.WhenAll(listeners.Select(listener => listener.Stop(cancellationToken).AsTask()));
    }
}
