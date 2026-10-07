using Whispr.RabbitMq.Management;

namespace Whispr.RabbitMq.Tests.Management;

public sealed class DelayInfrastructureTests
{
    [Fact]
    public void Given_Delay_When_GetRoutingKey_Then_ReturnsBitsMostSignificantFirstFollowedByTopicName()
    {
        // Act
        var routingKey = DelayInfrastructure.GetRoutingKey(5, "my-topic");

        // Assert
        Assert.Equal($"{string.Concat(Enumerable.Repeat("0.", 25))}1.0.1.my-topic", routingKey);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(60)]
    [InlineData(86_400)]
    [InlineData(123_456_789)]
    [InlineData(DelayInfrastructure.MaxDelaySeconds)]
    public void Given_Delay_When_RoutedThroughLevels_Then_WaitsForDelayAndReachesTopic(long delaySeconds)
    {
        // Arrange
        var routingKey = DelayInfrastructure.GetRoutingKey(delaySeconds, "my.topic");

        // Act - Mimic the broker, which routes the message through every level, to the queue or the next level
        var waitedSeconds = 0L;
        for (var level = DelayInfrastructure.LevelCount - 1; level >= 0; level--)
        {
            var toQueue = Matches(DelayInfrastructure.GetLevelBindingKey(level, '1'), routingKey);
            var toNextLevel = Matches(DelayInfrastructure.GetLevelBindingKey(level, '0'), routingKey);
            Assert.NotEqual(toQueue, toNextLevel);

            if (toQueue)
                waitedSeconds += 1L << level;
        }

        // Assert
        Assert.Equal(delaySeconds, waitedSeconds);
        Assert.True(Matches(DelayInfrastructure.GetDeliveryBindingKey("my.topic"), routingKey));
    }

    [Theory]
    [InlineData("topic")]
    [InlineData("other.my.topic")]
    [InlineData("my.topic.other")]
    public void Given_OtherTopic_When_MatchingDeliveryBindingKey_Then_DoesNotMatch(string otherTopicName)
    {
        // Arrange
        var routingKey = DelayInfrastructure.GetRoutingKey(5, otherTopicName);

        // Act
        var matches = Matches(DelayInfrastructure.GetDeliveryBindingKey("my.topic"), routingKey);

        // Assert
        Assert.False(matches);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-10_000, 0)]
    [InlineData(1, 1)]
    [InlineData(1_000, 1)]
    [InlineData(1_001, 2)]
    [InlineData(60_000, 60)]
    public void Given_DeferredUntil_When_GetDelaySeconds_Then_RoundsUp(int deferredUntilOffsetMilliseconds, long expected)
    {
        // Arrange
        var now = new DateTimeOffset(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);

        // Act
        var delaySeconds = DelayInfrastructure.GetDelaySeconds(now.AddMilliseconds(deferredUntilOffsetMilliseconds), now);

        // Assert
        Assert.Equal(expected, delaySeconds);
    }

    // Matches a routing key against a binding key of a topic exchange, where '*' matches one word and '#' matches zero or more words
    private static bool Matches(string bindingKey, string routingKey)
    {
        return Matches(bindingKey.Split('.'), routingKey.Split('.'));

        static bool Matches(ReadOnlySpan<string> pattern, ReadOnlySpan<string> words)
        {
            if (pattern.IsEmpty)
                return words.IsEmpty;

            if (pattern[0] == "#")
            {
                for (var skip = 0; skip <= words.Length; skip++)
                {
                    if (Matches(pattern[1..], words[skip..]))
                        return true;
                }

                return false;
            }

            return !words.IsEmpty
                && (pattern[0] == "*" || pattern[0] == words[0])
                && Matches(pattern[1..], words[1..]);
        }
    }
}
