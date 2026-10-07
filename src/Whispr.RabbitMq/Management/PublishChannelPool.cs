using System.Collections.Concurrent;

namespace Whispr.RabbitMq.Management;

/// <summary>
/// Pool of channels with publisher confirms, so concurrent publishes don't share a channel.
/// </summary>
internal sealed class PublishChannelPool(ConnectionProvider connectionProvider) : IAsyncDisposable
{
    private static readonly CreateChannelOptions ChannelOptions = new(
        publisherConfirmationsEnabled: true,
        publisherConfirmationTrackingEnabled: true);

    private readonly ConcurrentBag<IChannel> _channels = [];

    public async ValueTask Use(Func<IChannel, ValueTask> action, CancellationToken cancellationToken = default)
    {
        var channel = await Rent(cancellationToken);
        try
        {
            await action(channel);
        }
        finally
        {
            await Return(channel);
        }
    }

    private async ValueTask<IChannel> Rent(CancellationToken cancellationToken)
    {
        while (_channels.TryTake(out var channel))
        {
            if (channel.IsOpen)
                return channel;

            await channel.DisposeAsync();
        }

        var connection = await connectionProvider.GetConnection(cancellationToken);
        return await connection.CreateChannelAsync(ChannelOptions, cancellationToken);
    }

    private async ValueTask Return(IChannel channel)
    {
        // The broker closes a channel on errors, e.g. when publishing to an exchange that doesn't exist
        if (channel.IsOpen)
            _channels.Add(channel);
        else
            await channel.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        while (_channels.TryTake(out var channel))
            await channel.DisposeAsync();
    }
}
