using Whispr.AzureServiceBus.Transport;

namespace Whispr.AzureServiceBus.Tests.Transport;

public sealed class ServiceBusTransportTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(5, 16)]
    [InlineData(6, 30)]
    [InlineData(1_000, 30)]
    public void Given_DeliveryCount_When_GetRetryDelay_Then_ReturnsExponentialDelayCappedAtMax(int deliveryCount, int expectedSeconds)
    {
        // Act
        var delay = ServiceBusTransport.GetRetryDelay(deliveryCount, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }

    [Fact]
    public void Given_ZeroBackoffBase_When_GetRetryDelay_Then_ReturnsZero()
    {
        // Act
        var delay = ServiceBusTransport.GetRetryDelay(3, TimeSpan.Zero, TimeSpan.FromSeconds(30));

        // Assert
        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Theory]
    [InlineData(60, false)]
    [InlineData(0, false)]
    [InlineData(-5, false)]
    [InlineData(-6, true)]
    [InlineData(-60, true)]
    public void Given_LockedUntil_When_IsLockExpired_Then_OnlyReturnsTrueBeyondClockSkewTolerance(int lockedUntilOffsetSeconds, bool expected)
    {
        // Arrange
        var now = new DateTimeOffset(2026, 10, 2, 12, 0, 0, TimeSpan.Zero);
        var lockedUntil = now.AddSeconds(lockedUntilOffsetSeconds);

        // Act
        var expired = ServiceBusTransport.IsLockExpired(lockedUntil, now);

        // Assert
        Assert.Equal(expected, expired);
    }
}
