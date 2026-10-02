using Whispr.AzureServiceBus.Transport;

namespace Whispr.AzureServiceBus.Tests.Transport;

public sealed class PendingCompletionsTests
{
    [Fact]
    public async Task Given_FreeSlot_When_Track_Then_ReturnsBeforeTheCompletionFinishes()
    {
        // Arrange
        var pendingCompletions = new PendingCompletions(maxPending: 1);
        var completion = new TaskCompletionSource();

        // Act
        await pendingCompletions.Track(() => completion.Task).AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, pendingCompletions.Count);
        completion.SetResult();
    }

    [Fact]
    public async Task Given_MaxPendingReached_When_Track_Then_CompletesInline()
    {
        // Arrange
        var pendingCompletions = new PendingCompletions(maxPending: 1);
        var first = new TaskCompletionSource();
        var second = new TaskCompletionSource();
        await pendingCompletions.Track(() => first.Task);

        // Act
        var track = pendingCompletions.Track(() => second.Task).AsTask();
        await Task.Delay(50, TestContext.Current.CancellationToken);
        var returnedBeforeItsOwnCompletion = track.IsCompleted;
        second.SetResult();
        await track.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(returnedBeforeItsOwnCompletion);
        Assert.False(first.Task.IsCompleted);
        Assert.Equal(1, pendingCompletions.Count);
        first.SetResult();
    }

    [Fact]
    public async Task Given_PendingCompletions_When_Drain_Then_WaitsForAllOfThem()
    {
        // Arrange
        var pendingCompletions = new PendingCompletions(maxPending: 4);
        var completions = Enumerable.Range(0, 3).Select(_ => new TaskCompletionSource()).ToArray();
        foreach (var completion in completions)
            await pendingCompletions.Track(() => completion.Task);

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
    public async Task Given_CompletionTrackedWhileDraining_When_Drain_Then_WaitsForItToo()
    {
        // Arrange
        var pendingCompletions = new PendingCompletions(maxPending: 2);
        var first = new TaskCompletionSource();
        var second = new TaskCompletionSource();
        await pendingCompletions.Track(() => first.Task);
        var drain = pendingCompletions.Drain(TestContext.Current.CancellationToken).AsTask();

        // Act
        await pendingCompletions.Track(() => second.Task);
        first.SetResult();
        await Task.Delay(50, TestContext.Current.CancellationToken);
        var drainedEarly = drain.IsCompleted;
        second.SetResult();
        await drain.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(drainedEarly);
    }

    [Fact]
    public async Task Given_FailedCompletion_When_Drain_Then_DoesNotThrowAndReleasesTheSlot()
    {
        // Arrange
        var pendingCompletions = new PendingCompletions(maxPending: 1);
        var failing = new TaskCompletionSource();
        await pendingCompletions.Track(() => failing.Task);
        var drain = pendingCompletions.Drain(TestContext.Current.CancellationToken).AsTask();

        // Act
        failing.SetException(new InvalidOperationException("lock lost"));
        var exception = await Record.ExceptionAsync(() => drain.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        var next = new TaskCompletionSource();
        await pendingCompletions.Track(() => next.Task).AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(exception);
        Assert.Equal(1, pendingCompletions.Count);
        next.SetResult();
    }

    [Fact]
    public async Task Given_CompleteThrowsSynchronously_When_Track_Then_RethrowsAndReleasesTheSlot()
    {
        // Arrange
        var pendingCompletions = new PendingCompletions(maxPending: 1);

        // Act
        var exception = await Record.ExceptionAsync(async () => await pendingCompletions.Track(() => throw new InvalidOperationException()));
        var next = new TaskCompletionSource();
        await pendingCompletions.Track(() => next.Task).AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal(1, pendingCompletions.Count);
        next.SetResult();
    }

    [Fact]
    public async Task Given_CancelledToken_When_Drain_Then_ReturnsWithoutWaiting()
    {
        // Arrange
        var pendingCompletions = new PendingCompletions(maxPending: 1);
        await pendingCompletions.Track(() => new TaskCompletionSource().Task);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act
        await pendingCompletions.Drain(cancellation.Token).AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, pendingCompletions.Count);
    }
}
