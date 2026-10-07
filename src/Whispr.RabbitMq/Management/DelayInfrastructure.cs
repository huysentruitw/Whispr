using System.Text;

namespace Whispr.RabbitMq.Management;

/// <summary>
/// Delays messages without the delayed message exchange plugin, by routing them through a cascade of delay levels.
/// </summary>
/// <remarks>
/// Level N consists of a topic exchange and a queue in which messages expire after 2^N seconds. The delay in seconds
/// is encoded in the routing key as one word per bit, most significant bit first, followed by the topic name, e.g.
/// <c>0.0.(...).1.0.1.my-topic</c> for a delay of 5 seconds. The exchange of level N routes a message to its queue
/// when bit N is set, or to the exchange of the next level otherwise. An expired message is dead-lettered to the
/// exchange of the next level, so it waits 2^N seconds for each bit set. After level 0, the message reaches the
/// delivery exchange, which routes it to the exchange of its topic.
/// </remarks>
internal static class DelayInfrastructure
{
    public const int LevelCount = 28;

    /// <summary>
    /// The maximum delay of about 8.5 years.
    /// </summary>
    public const long MaxDelaySeconds = (1L << LevelCount) - 1;

    public const string DeliveryExchangeName = "whispr.delay-delivery";

    public static string EntryExchangeName => GetLevelName(LevelCount - 1);

    public static string GetLevelName(int level) => $"whispr.delay-level-{level:D2}";

    public static long GetDelaySeconds(DateTimeOffset deferredUntil, DateTimeOffset now)
    {
        // Round up, so a message is never delivered before the moment it was deferred until
        var delay = deferredUntil - now;
        return delay <= TimeSpan.Zero ? 0 : (long)Math.Ceiling(delay.TotalSeconds);
    }

    public static string GetRoutingKey(long delaySeconds, string topicName)
    {
        var routingKey = new StringBuilder(LevelCount * 2 + topicName.Length);

        for (var level = LevelCount - 1; level >= 0; level--)
            routingKey.Append(((delaySeconds >> level) & 1) == 1 ? '1' : '0').Append('.');

        return routingKey.Append(topicName).ToString();
    }

    /// <summary>
    /// The binding key that matches routing keys with the given bit of the delay set (<c>1</c>) or not set (<c>0</c>).
    /// </summary>
    public static string GetLevelBindingKey(int level, char bit)
        => $"{string.Concat(Enumerable.Repeat("*.", LevelCount - 1 - level))}{bit}.#";

    /// <summary>
    /// The binding key that matches routing keys of the given topic, whatever the delay.
    /// </summary>
    public static string GetDeliveryBindingKey(string topicName)
        => $"{string.Concat(Enumerable.Repeat("*.", LevelCount))}{topicName}";

    public static async Task Declare(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(DeliveryExchangeName, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);

        // Declare all exchanges first, as binding exchanges to each other requires both to exist
        for (var level = LevelCount - 1; level >= 0; level--)
            await channel.ExchangeDeclareAsync(GetLevelName(level), ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);

        for (var level = LevelCount - 1; level >= 0; level--)
        {
            var levelName = GetLevelName(level);
            var nextExchangeName = level == 0 ? DeliveryExchangeName : GetLevelName(level - 1);

            await channel.QueueDeclareAsync(
                levelName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                    ["x-queue-type"] = "quorum",
                    ["x-message-ttl"] = (1L << level) * 1000,
                    // Keeps the routing key, so the next level can route on the next bit
                    ["x-dead-letter-exchange"] = nextExchangeName,
                },
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(levelName, levelName, GetLevelBindingKey(level, '1'), cancellationToken: cancellationToken);
            await channel.ExchangeBindAsync(nextExchangeName, levelName, GetLevelBindingKey(level, '0'), cancellationToken: cancellationToken);
        }
    }
}
