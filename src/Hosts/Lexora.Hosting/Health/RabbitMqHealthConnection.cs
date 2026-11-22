using RabbitMQ.Client;

namespace Lexora.Hosting.Health;

internal sealed class RabbitMqHealthConnection(Uri uri) : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetAsync()
    {
        if (_connection is { } existing)
        {
            return existing;
        }

        await _gate.WaitAsync();

        try
        {
            // A failed attempt is not kept, so the next probe tries again once the broker is back.
            return _connection ??= await new ConnectionFactory { Uri = uri }.CreateConnectionAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _gate.Dispose();
    }
}
