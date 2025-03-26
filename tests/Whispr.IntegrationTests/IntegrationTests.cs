using Microsoft.Extensions.Hosting;
using Whispr.IntegrationTests.Tests.TestInfrastructure;

namespace Whispr.IntegrationTests.Tests;

public sealed class IntegrationTests(SqlServerFixture sqlServerFixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Given_MessageHandlerRegistered_When_MessagePublished_Then_MessageHandled(bool useOutbox)
    {
        // Arrange
        using var host = await HostFactory.Create(sqlServerFixture.ConnectionString, useOutbox);
        var message = new ChirpHeard($"Robin {Guid.NewGuid():N}", TimeSpan.FromSeconds(1));

        // Act
        await MimicAction(host, message);

        // Assert
        var handledMessage = ChirpHandler.WaitForMessage<ChirpHeard>(m => m.BirdName == message.BirdName, TimeSpan.FromSeconds(10));
        Assert.NotNull(handledMessage);
    }

    private static async ValueTask MimicAction(IHost host, ChirpHeard message)
    {
        using var serviceScope = host.Services.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<DataContext>();
        var messagePublisher = serviceScope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        dbContext.Set<Product>().Add(new Product { Id = Guid.NewGuid(), Name = "Test", Price = 1.23m });
        await messagePublisher.Publish(message, cancellationToken: TestContext.Current.CancellationToken);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
