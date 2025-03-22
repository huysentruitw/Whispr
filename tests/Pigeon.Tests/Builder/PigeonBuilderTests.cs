using Microsoft.Extensions.DependencyInjection;
using Pigeon.Builder;

namespace Pigeon.Tests.Builder;

public sealed class PigeonBuilderTests
{
    [Fact]
    public void Given_Construct_Then_RegistersMessageHandlerDescriptors()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new PigeonBuilder(services);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var descriptors = serviceProvider.GetServices<MessageHandlerDescriptor>();

        // Assert
        Assert.NotNull(descriptors);
        Assert.Equal(builder.MessageHandlerDescriptors, descriptors);
    }
}
