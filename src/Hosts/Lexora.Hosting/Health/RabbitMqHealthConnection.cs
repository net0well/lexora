using RabbitMQ.Client;

namespace Lexora.Hosting.Health;

internal sealed class RabbitMqHealthConnection(Uri uri) : IAsyncDisposable
{
    private readonly Lazy<Task<IConnection>> _connection = new(() => new ConnectionFactory { Uri = uri }.CreateConnectionAsync());

    public Task<IConnection> GetAsync() => _connection.Value;

    public async ValueTask DisposeAsync()
    {
        if (_connection.IsValueCreated && _connection.Value.IsCompletedSuccessfully)
        {
            await _connection.Value.Result.DisposeAsync();
        }
    }
}
