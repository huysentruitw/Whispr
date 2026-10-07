using System.Collections.Concurrent;

namespace Whispr.RabbitMq.Management;

/// <summary>
/// Declares a fanout exchange per topic, and a quorum queue per handler that is bound to the exchanges of its topics.
/// </summary>
internal sealed class TopologyManager(ConnectionProvider connectionProvider)
{
    private readonly ConcurrentDictionary<string, byte> _declaredExchanges = new();
    private readonly ConcurrentDictionary<string, byte> _delayBoundExchanges = new();
    private volatile bool _delayInfrastructureDeclared;

    public static string GetDeadLetterQueueName(string queueName) => $"{queueName}.dead-letter";

    public async ValueTask DeclareExchangeIfNotDeclared(IChannel channel, string topicName, CancellationToken cancellationToken = default)
    {
        if (_declaredExchanges.ContainsKey(topicName))
            return;

        // Declaring is idempotent, so concurrent declarations of the same exchange are harmless
        await DeclareExchange(channel, topicName, cancellationToken);
    }

    /// <summary>
    /// Declares the delay infrastructure, and binds the exchange of the topic to its delivery exchange.
    /// </summary>
    public async ValueTask DeclareDelayInfrastructureIfNotDeclared(IChannel channel, string topicName, CancellationToken cancellationToken = default)
    {
        if (!_delayInfrastructureDeclared)
        {
            await DelayInfrastructure.Declare(channel, cancellationToken);
            _delayInfrastructureDeclared = true;
        }

        if (_delayBoundExchanges.ContainsKey(topicName))
            return;

        await channel.ExchangeBindAsync(
            destination: topicName,
            source: DelayInfrastructure.DeliveryExchangeName,
            routingKey: DelayInfrastructure.GetDeliveryBindingKey(topicName),
            cancellationToken: cancellationToken);

        _delayBoundExchanges.TryAdd(topicName, 0);
    }

    /// <summary>
    /// Forgets what was declared for the topic, so it is declared again, e.g. after it was deleted from the broker.
    /// </summary>
    public void ForgetTopic(string topicName)
    {
        _declaredExchanges.TryRemove(topicName, out _);
        _delayBoundExchanges.TryRemove(topicName, out _);
        _delayInfrastructureDeclared = false;
    }

    public async ValueTask DeclareQueue(string queueName, string[] topicNames, CancellationToken cancellationToken = default)
    {
        var connection = await connectionProvider.GetConnection(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var deadLetterQueueName = GetDeadLetterQueueName(queueName);
        await channel.QueueDeclareAsync(
            deadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },
            cancellationToken: cancellationToken);

        // These arguments must not depend on options, as the broker refuses to redeclare a queue with other arguments
        await channel.QueueDeclareAsync(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-queue-type"] = "quorum",
                // The transport dead-letters failed messages itself. This only catches messages that exceed the
                // delivery limit of the broker, e.g. because the consumer keeps crashing while handling them.
                ["x-dead-letter-exchange"] = string.Empty,
                ["x-dead-letter-routing-key"] = deadLetterQueueName,
            },
            cancellationToken: cancellationToken);

        foreach (var topicName in topicNames)
        {
            await DeclareExchange(channel, topicName, cancellationToken);
            await channel.QueueBindAsync(queueName, topicName, routingKey: string.Empty, cancellationToken: cancellationToken);
        }

        await channel.CloseAsync(cancellationToken);
    }

    private async Task DeclareExchange(IChannel channel, string topicName, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(topicName, ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: cancellationToken);
        _declaredExchanges.TryAdd(topicName, 0);
    }
}
