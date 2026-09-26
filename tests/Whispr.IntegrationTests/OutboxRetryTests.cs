using Whispr.EntityFrameworkCore.Entities;
using Whispr.IntegrationTests.TestInfrastructure;

namespace Whispr.IntegrationTests;

public sealed class OutboxRetryTests(HostFixture hostFixture)
{
    [Fact]
    public async Task Given_SendFailing_When_MessagePublished_Then_MessageParkedAfterMaxSendAttempts()
    {
        // Arrange
        var featherId = Guid.NewGuid();

        // Act
        await Publish(new FeatherDropped(featherId));

        // Assert
        var outboxMessage = await WaitForOutboxMessage(
            featherId,
            m => m.ParkedAtUtc is not null,
            TimeSpan.FromSeconds(30));

        Assert.NotNull(outboxMessage);
        Assert.Equal(3, outboxMessage.AttemptCount);
        Assert.Null(outboxMessage.ProcessedAtUtc);
        Assert.Contains("Sending FeatherDropped always fails", outboxMessage.LastError);
    }

    [Fact]
    public async Task Given_FullBatchOfFailingMessages_When_OtherMessagePublished_Then_OtherMessageStillDelivered()
    {
        // Arrange
        var failingMessages = Enumerable.Range(0, 100)
            .Select(_ => new FeatherDropped(Guid.NewGuid()))
            .ToArray();
        var birdId = Guid.NewGuid();

        // Act
        await Publish([..failingMessages, new ChirpHeard(birdId)]);

        // Assert
        var handledMessage = ChirpHandler.WaitForMessage<ChirpHeard>(m => m.BirdId == birdId, TimeSpan.FromSeconds(30));
        Assert.NotNull(handledMessage);
    }

    private async ValueTask Publish(params object[] messages)
    {
        using var serviceScope = hostFixture.CreateScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<DataContext>();
        var messagePublisher = serviceScope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        foreach (var message in messages)
            await messagePublisher.Publish(message, cancellationToken: TestContext.Current.CancellationToken);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async ValueTask<OutboxMessage?> WaitForOutboxMessage(Guid featherId, Func<OutboxMessage, bool> predicate, TimeSpan timeout)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(timeout);

        while (!cts.Token.IsCancellationRequested)
        {
            using var serviceScope = hostFixture.CreateScope();
            var dbContext = serviceScope.ServiceProvider.GetRequiredService<DataContext>();

            var outboxMessage = await dbContext.Set<OutboxMessage>()
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.Body.Contains(featherId.ToString()), CancellationToken.None);

            if (outboxMessage is not null && predicate(outboxMessage))
                return outboxMessage;

            await Task.Delay(100, CancellationToken.None);
        }

        return null;
    }
}
