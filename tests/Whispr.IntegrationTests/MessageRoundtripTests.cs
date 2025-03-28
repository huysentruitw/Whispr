using System.Diagnostics;
using Whispr.IntegrationTests.TestInfrastructure;

namespace Whispr.IntegrationTests.Tests;

public sealed class MessageRoundtripTests(HostFixture hostFixture)
{
    [Fact]
    public async Task Given_MessageHandlerRegistered_When_MessagePublished_Then_MessageHandled()
    {
        // Arrange
        var birdId = Guid.NewGuid();
        var message = new ChirpHeard(BirdId: birdId, TimeUtc: DateTime.UtcNow);

        // Act
        await MimicAction(hostFixture, message);

        // Assert
        var handledMessage = ChirpHandler.WaitForMessage<ChirpHeard>(m => m.BirdId == birdId, TimeSpan.FromSeconds(10));
        Assert.NotNull(handledMessage);
    }

    [Theory]
    [InlineData(500)]
    public async Task Given_MessageHandlerRegistered_When_PublishLotsOfMessages_Then_MessagesHandledInTime(int iterations)
    {
        // Arrange
        var messages = Enumerable.Range(0, iterations)
            .Select(_ => new ChirpHeard(BirdId: Guid.NewGuid(), TimeUtc: DateTime.UtcNow))
            .ToArray();

        // Act
        await MimicAction(hostFixture, messages);

        var handledMessages = messages.Select(message => ChirpHandler.WaitForMessage<ChirpHeard>(m => m.BirdId == message.BirdId, TimeSpan.FromSeconds(10)))
            .ToArray();

        Assert.All(handledMessages, Assert.NotNull);
    }

    private static async ValueTask MimicAction(IServiceProvider serviceProvider, params ChirpHeard[] messages)
    {
        using var serviceScope = serviceProvider.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<DataContext>();
        var messagePublisher = serviceScope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        dbContext.Set<Product>().Add(new Product { Id = Guid.NewGuid(), Name = "Test", Price = 1.23m });

        Activity.Current = new Activity("Test");

        foreach (var message in messages)
            await messagePublisher.Publish(message, cancellationToken: TestContext.Current.CancellationToken);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
