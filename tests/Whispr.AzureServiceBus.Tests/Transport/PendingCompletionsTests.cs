using Whispr.AzureServiceBus.Transport;

namespace Whispr.AzureServiceBus.Tests.Transport;

public sealed class PendingCompletionsTests
{
    [Fact]
    public async Task Given_MaxPendingReached_When_Track_Then_WaitsUntilACompletionFinishes()
    {
        // Arrange
        var pendingCompletions = new PendingCompletions(maxPending: 1, onFailed: (_, _) => { });
        var first = new TaskCompletionSource();
        await pendingCompletions.Track(() => first.Task, "first");

        // Act
        var second = pendingCompletions.Track(() => Task.CompletedTask, "second").AsTask();
        await Task.Delay(50, TestContext.Current.CancellationToken);
        var startedBeforeFirstFinished = second.IsCompleted;
        first.SetResult();
        await second.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(startedBeforeFirstFinished);
        Assert.True(second.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Given_PendingCompletions_When_Drain_Then_WaitsForAllOfThem()
    {
        // Arrange
        var pendingCompletions = new PendingCompletions(maxPending: 4, onFailed: (_, _) => { });
        var completions = Enumerable.Range(0, 3).Select(_ => new TaskCompletionSource()).ToArray();
        foreach (var completion in completions)
            await pendingCompletions.Track(() => completion.Task, "message");

        // Act
        var drain = pendingCompletions.Drain(TestContext.Current.CancellationToken).AsTask();
        await Task.Delay(50, TestContext.Current.CancellationToken);
        var drainedEarly = drain.IsCompleted;
        foreach (var completion in completions)
            completion.SetResult();
        await drain.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(drainedEarly);
        Assert.Equal(0, pendingCompletions.Count);
    }

    [Fact]
    public async Task Given_FailingCompletion_When_ItFails_Then_ReportsTheMessageAndReleasesTheSlot()
    {
        // Arrange
        var failures = new List<(Exception Exception, string MessageId)>();
        var pendingCompletions = new PendingCompletions(maxPending: 1, onFailed: (exception, messageId) => failures.Add((exception, messageId)));

        // Act
        await pendingCompletions.Track(() => Task.FromException(new InvalidOperationException("lock lost")), "message-1");
        await pendingCompletions.Track(() => Task.CompletedTask, "message-2").AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        var failure = Assert.Single(failures);
        Assert.Equal("message-1", failure.MessageId);
        Assert.IsType<InvalidOperationException>(failure.Exception);
    }

    [Fact]
    public async Task Given_CompleteThrowsSynchronously_When_Track_Then_RethrowsAndReleasesTheSlot()
    {
        // Arrange
        var pendingCompletions = new PendingCompletions(maxPending: 1, onFailed: (_, _) => { });

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await pendingCompletions.Track(() => throw new InvalidOperationException(), "message-1"));
        await pendingCompletions.Track(() => Task.CompletedTask, "message-2").AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(0, pendingCompletions.Count);
    }

    [Fact]
    public async Task Given_CancelledToken_When_Drain_Then_ReturnsWithoutWaiting()
    {
        // Arrange
        var pendingCompletions = new PendingCompletions(maxPending: 1, onFailed: (_, _) => { });
        await pendingCompletions.Track(() => new TaskCompletionSource().Task, "message");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act
        await pendingCompletions.Drain(cancellation.Token).AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, pendingCompletions.Count);
    }
}
