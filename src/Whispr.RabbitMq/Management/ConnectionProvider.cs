namespace Whispr.RabbitMq.Management;

internal sealed class ConnectionProvider(RabbitMqOptions options) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async ValueTask<IConnection> GetConnection(CancellationToken cancellationToken = default)
    {
        if (_connection is { } connection)
            return connection;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            return _connection ??= await CreateConnection(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IConnection> CreateConnection(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(options.ConnectionString))
            throw new InvalidOperationException("ConnectionString must be provided.");

        var factory = new ConnectionFactory
        {
            Uri = new Uri(options.ConnectionString),
            ClientProvidedName = options.ClientProvidedName,
            // Recovers the connection, channels and consumers after a network failure
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
        };

        return await factory.CreateConnectionAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }
}
