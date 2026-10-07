using RabbitMQ.Client.Exceptions;

namespace Whispr.RabbitMq.Transport;

/// <summary>
/// A consumer on its own channel, which tracks the messages in flight so it can stop gracefully.
/// </summary>
internal sealed class Listener(string queueName, IChannel channel)
{
    private readonly CancellationTokenSource _stoppingTokenSource = new();
    private readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Starts at one for the listener itself, which is released when stopping, so it reaches zero once drained
    private int _activeCount = 1;
    private int _stopped;
    private string? _consumerTag;

    public string QueueName { get; } = queueName;

    public IChannel Channel { get; } = channel;

    /// <summary>
    /// Signaled when the listener is stopping.
    /// </summary>
    public CancellationToken StoppingToken => _stoppingTokenSource.Token;

    public async ValueTask Start(IAsyncBasicConsumer consumer, CancellationToken cancellationToken)
        => _consumerTag = await Channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, cancellationToken);

    /// <summary>
    /// Registers a message in flight. Returns <see langword="false"/> when the listener has stopped already.
    /// </summary>
    public bool TryEnter()
    {
        while (true)
        {
            var activeCount = Volatile.Read(ref _activeCount);
            if (activeCount == 0)
                return false;

            if (Interlocked.CompareExchange(ref _activeCount, activeCount + 1, activeCount) == activeCount)
                return true;
        }
    }

    public void Exit()
    {
        if (Interlocked.Decrement(ref _activeCount) == 0)
            _drained.TrySetResult();
    }

    public async ValueTask Stop(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _stopped, 1) == 1)
            return;

        try
        {
            // Stop receiving new messages, then wait for the messages in flight to be settled
            await CancelConsumer(cancellationToken);
            await _stoppingTokenSource.CancelAsync();
            Exit();

            await _drained.Task.WaitAsync(cancellationToken);
        }
        finally
        {
            // Unsettled messages are made available again when the channel closes
            await Channel.DisposeAsync();
        }
    }

    private async ValueTask CancelConsumer(CancellationToken cancellationToken)
    {
        if (_consumerTag is null)
            return;

        try
        {
            await Channel.BasicCancelAsync(_consumerTag, noWait: false, cancellationToken);
        }
        catch (OperationInterruptedException)
        {
            // The channel is closed already, so no new messages are received either
        }
    }
}
