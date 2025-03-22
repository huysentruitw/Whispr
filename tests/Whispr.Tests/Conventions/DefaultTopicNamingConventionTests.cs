using Whispr.Conventions;

namespace Whispr.Tests.Conventions;

public sealed class DefaultTopicNamingConventionTests
{
    [Fact]
    public void Given_SimpleType_When_Format_Then_ReturnsFormattedTopic()
    {
        // Arrange
        var convention = new DefaultTopicNamingConvention();
        var messageType = typeof(SimpleMessage);

        // Act
        var result = convention.Format(messageType);

        // Assert
        Assert.Equal("whispr--tests--conventions--simple-message", result);
    }

    [Fact]
    public void Given_DifferentTypes_When_Format_Then_ReturnsDifferentTopics()
    {
        // Arrange
        var convention = new DefaultTopicNamingConvention();
        var messageType1 = typeof(SimpleMessage);
        var messageType2 = typeof(OtherMessage);

        // Act
        var result1 = convention.Format(messageType1);
        var result2 = convention.Format(messageType2);

        // Assert
        Assert.NotEqual(result1, result2);
    }

    private record SimpleMessage(string Content);
    private record OtherMessage(int Id);
}

