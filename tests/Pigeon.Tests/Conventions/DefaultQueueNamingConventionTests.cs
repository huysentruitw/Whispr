﻿using Pigeon.Conventions;

namespace Pigeon.Tests.Conventions;

public sealed class DefaultQueueNamingConventionTests
{
    [Fact]
    public void Given_HandlerType_When_Format_Then_ReturnsFormattedQueue()
    {
        // Arrange
        var convention = new DefaultQueueNamingConvention();
        var handlerType = typeof(SimpleHandler);

        // Act
        var result = convention.Format(handlerType);

        // Assert
        Assert.Equal("pigeon--tests--conventions--simple-handler", result);
    }

    private sealed class SimpleHandler();
}
