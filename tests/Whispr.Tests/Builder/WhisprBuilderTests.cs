using Microsoft.Extensions.DependencyInjection;
using Whispr.Builder;
using Whispr.Descriptors;

namespace Whispr.Tests.Builder;

public sealed class WhisprBuilderTests
{
    [Fact]
    public void Given_Construct_Then_RegistersMessageHandlerDescriptors()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new WhisprBuilder(services);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var descriptors = serviceProvider.GetServices<MessageHandlerDescriptor>();

        // Assert
        Assert.NotNull(descriptors);
        Assert.Equal(builder.MessageHandlerDescriptors, descriptors);
        Assert.Equal(builder.Services, services);
    }
}
