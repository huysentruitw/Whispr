using Whispr.IntegrationTests.TestInfrastructure;

namespace Whispr.IntegrationTests;

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
        var handledMessage = ChirpHandler.WaitForMessage<ChirpHeard>(m => m.BirdId == birdId, TimeSpan.FromSeconds(20));
        Assert.NotNull(handledMessage);
    }

    [Fact]
    public async Task Given_MessageHandlerRegistered_When_PublishLotsOfMessages_Then_MessagesHandledInTime()
    {
        // Arrange
        var iterationCount = CiDetector.IsCi() ? 100 : 500;
        var messages = Enumerable.Range(0, iterationCount)
            .Select(_ => new ChirpHeard(BirdId: Guid.NewGuid(), TimeUtc: DateTime.UtcNow))
            .ToArray();

        // Act
        await MimicAction(hostFixture, messages);

        var handledMessages = messages
            .Select(message => ChirpHandler.WaitForMessage<ChirpHeard>(m => m.BirdId == message.BirdId, TimeSpan.FromSeconds(20)))
            .ToArray();

        Assert.All(handledMessages, Assert.NotNull);
    }

    [Fact]
    public async Task Given_MessageHandlerRegistered_When_DeferredMessagePublished_Then_MessageHandledAfterDeferral()
    {
        // Arrange
        var birdId = Guid.NewGuid();
        var message = new ChirpHeard(BirdId: birdId, TimeUtc: DateTime.UtcNow);
        var deferredUntil = DateTimeOffset.UtcNow.AddSeconds(3);

        // Act
        await MimicAction(hostFixture, options => options.Defer(deferredUntil), message);

        // Assert
        var handledMessage = ChirpHandler.WaitForMessage<ChirpHeard>(m => m.BirdId == birdId, TimeSpan.FromSeconds(20));
        Assert.NotNull(handledMessage);

        // Allow for clock skew with the broker
        var processedTime = ChirpHandler.GetMessageProcessedTime(birdId);
        Assert.True(processedTime >= deferredUntil.UtcDateTime.AddSeconds(-1), $"Processed at {processedTime:O}, deferred until {deferredUntil:O}");
    }

    [Fact]
    public async Task Given_MessageHandlerThrowingException_When_MessagePublished_Then_HandlerRetried()
    {
        // Arrange
        var nestId = Guid.NewGuid();
        var message = new NestBuilt(NestId: nestId);

        // Act
        await MimicAction(hostFixture, message);

        // Assert
        var handledMessage = NestHandler.WaitForMessage<NestBuilt>(
            m => m.NestId == nestId && NestHandler.GetMessageDeliveryCount(m.NestId) == 5,
            TimeSpan.FromSeconds(20));

        Assert.NotNull(handledMessage);
    }

    private static ValueTask MimicAction<TMessage>(IServiceProvider serviceProvider, params TMessage[] messages)
        where TMessage : class
        => MimicAction(serviceProvider, configure: null, messages);

    private static async ValueTask MimicAction<TMessage>(IServiceProvider serviceProvider, Action<PublishOptions>? configure, params TMessage[] messages)
        where TMessage : class
    {
        using var serviceScope = serviceProvider.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<DataContext>();
        var messagePublisher = serviceScope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        dbContext.Set<Product>().Add(new Product { Id = Guid.NewGuid(), Name = "Test", Price = 1.23m });

        foreach (var message in messages)
            await messagePublisher.Publish(message, configure, TestContext.Current.CancellationToken);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
