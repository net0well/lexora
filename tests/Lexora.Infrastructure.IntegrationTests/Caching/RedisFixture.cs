using Lexora.TestKit.Containers;
using Testcontainers.Redis;

namespace Lexora.Infrastructure.IntegrationTests.Caching;

public sealed class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _redis = RedisContainerFactory.Create();

    public string ConnectionString => _redis.GetConnectionString();

    public async ValueTask InitializeAsync() => await _redis.StartAsync(TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync() => await _redis.DisposeAsync();
}
