using Microsoft.Extensions.Logging;
using Whispr.Tests.TestInfrastructure;
using Whispr.Transport;

namespace Whispr.Tests.Transport;

public sealed class InMemoryTransportTests
{
    [Fact]
    public async Task Given_SenderCancellationTokenCancelled_When_Send_Then_ListenerReceivesUncancelledToken()
    {
        // Arrange
        var transport = new InMemoryTransport(Mock.Of<ILogger<InMemoryTransport>>());
        var receivedToken = new TaskCompletionSource<CancellationToken>();

        await transport.StartListener(
            "queue",
            ["topic"],
            (_, cancellationToken) =>
            {
                receivedToken.SetResult(cancellationToken);
                return ValueTask.CompletedTask;
            },
            TestContext.Current.CancellationToken);

        using var senderCancellation = new CancellationTokenSource();
        await senderCancellation.CancelAsync();

        // Act
        await transport.Send("topic", SerializedEnvelopeFactory.Create(new TestMessage("Test")), senderCancellation.Token);

        // Assert
        var cancellationToken = await receivedToken.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(cancellationToken.IsCancellationRequested);
    }

    private sealed record TestMessage(string Text);
}
