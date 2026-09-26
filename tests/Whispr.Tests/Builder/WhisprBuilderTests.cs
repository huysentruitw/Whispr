using System.Text.Json;
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
        var builder = new WhisprBuilder(services, "BusName");
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var descriptors = serviceProvider.GetServices<MessageHandlerDescriptor>();

        // Assert
        Assert.NotNull(descriptors);
        Assert.Equal(builder.MessageHandlerDescriptors, descriptors);
        Assert.Equal(builder.Services, services);
    }

    [Fact]
    public void Given_JsonSerializerOptionsConfiguredForOneBus_When_GetJsonSerializerOptions_Then_OnlyThatBusUsesThem()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddWhispr("BusA").ConfigureJsonSerializerOptions(options => options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
        services.AddWhispr("BusB");
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var busAOptions = serviceProvider.GetJsonSerializerOptions("BusA");
        var busBOptions = serviceProvider.GetJsonSerializerOptions("BusB");

        // Assert
        Assert.Equal(JsonNamingPolicy.CamelCase, busAOptions.PropertyNamingPolicy);
        Assert.Null(busBOptions.PropertyNamingPolicy);
    }
}
