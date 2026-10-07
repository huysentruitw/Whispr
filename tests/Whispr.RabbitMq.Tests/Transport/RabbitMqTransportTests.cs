using System.Text;
using Whispr.RabbitMq.Transport;

namespace Whispr.RabbitMq.Tests.Transport;

public sealed class RabbitMqTransportTests
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
        var delay = RabbitMqTransport.GetRetryDelay(deliveryCount, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }

    [Fact]
    public void Given_ZeroBackoffBase_When_GetRetryDelay_Then_ReturnsZero()
    {
        // Act
        var delay = RabbitMqTransport.GetRetryDelay(3, TimeSpan.Zero, TimeSpan.FromSeconds(30));

        // Assert
        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Fact]
    public void Given_NoHeaders_When_GetDeliveryCount_Then_ReturnsFirstDelivery()
    {
        // Act
        var deliveryCount = RabbitMqTransport.GetDeliveryCount(null);

        // Assert
        Assert.Equal(1, deliveryCount);
    }

    [Fact]
    public void Given_NoDeliveryCountHeader_When_GetDeliveryCount_Then_ReturnsFirstDelivery()
    {
        // Arrange
        var headers = new Dictionary<string, object?> { ["MessageType"] = "Foo"u8.ToArray() };

        // Act
        var deliveryCount = RabbitMqTransport.GetDeliveryCount(headers);

        // Assert
        Assert.Equal(1, deliveryCount);
    }

    [Theory]
    [InlineData(1L, 2)]
    [InlineData(4L, 5)]
    [InlineData(4, 5)]
    public void Given_DeliveryCountHeader_When_GetDeliveryCount_Then_ReturnsPreviousDeliveriesPlusOne(object previousDeliveries, int expected)
    {
        // Arrange
        var headers = new Dictionary<string, object?> { ["x-delivery-count"] = previousDeliveries };

        // Act
        var deliveryCount = RabbitMqTransport.GetDeliveryCount(headers);

        // Assert
        Assert.Equal(expected, deliveryCount);
    }

    [Fact]
    public void Given_ByteArrayHeader_When_GetStringHeader_Then_ReturnsDecodedString()
    {
        // Arrange
        var headers = new Dictionary<string, object?> { ["MessageType"] = Encoding.UTF8.GetBytes("My.Namespace.SomethingHappened") };

        // Act
        var value = RabbitMqTransport.GetStringHeader(headers, "MessageType");

        // Assert
        Assert.Equal("My.Namespace.SomethingHappened", value);
    }

    [Fact]
    public void Given_StringHeader_When_GetStringHeader_Then_ReturnsString()
    {
        // Arrange
        var headers = new Dictionary<string, object?> { ["MessageType"] = "My.Namespace.SomethingHappened" };

        // Act
        var value = RabbitMqTransport.GetStringHeader(headers, "MessageType");

        // Assert
        Assert.Equal("My.Namespace.SomethingHappened", value);
    }

    [Fact]
    public void Given_MissingHeader_When_GetStringHeader_Then_ReturnsNull()
    {
        // Act
        var value = RabbitMqTransport.GetStringHeader(new Dictionary<string, object?>(), "MessageType");

        // Assert
        Assert.Null(value);
    }
}
