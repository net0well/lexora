using Lexora.Application.Abstractions.Caching;
using Lexora.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lexora.Infrastructure.IntegrationTests.Caching;

public sealed class HybridCacheStoreTests(RedisFixture redis) : IClassFixture<RedisFixture>
{
    private static readonly TimeSpan _hour = TimeSpan.FromHours(1);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetOrCreateAsync_WithoutRedis_KeepsTheValueInMemory()
    {
        await using var services = Compose(redisConnection: null);
        var cache = services.GetRequiredService<ICacheStore>();
        var calls = 0;
        var key = Key();

        var first = await cache.GetOrCreateAsync(key, _ => ValueTask.FromResult(++calls), _hour, tags: null, CancellationToken);
        var second = await cache.GetOrCreateAsync(key, _ => ValueTask.FromResult(++calls), _hour, tags: null, CancellationToken);

        (first, calls).ShouldBe((1, 1));
        second.ShouldBe(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_SecondInstanceWithTheSameRedis_ReadsTheSharedValue()
    {
        var key = Key();
        await using var first = Compose(redis.ConnectionString);
        await using var second = Compose(redis.ConnectionString);

        await first.GetRequiredService<ICacheStore>().GetOrCreateAsync(key, _ => ValueTask.FromResult<float[]>([0.1f, 0.2f]), _hour, tags: null, CancellationToken);
        await WaitUntilStoredInRedisAsync(first, key);
        var shared = await second.GetRequiredService<ICacheStore>()
            .GetOrCreateAsync<float[]>(key, _ => throw new InvalidOperationException("The value should come from Redis."), _hour, tags: null, CancellationToken);

        shared.ShouldBe([0.1f, 0.2f]);
    }

    [Fact]
    public async Task RemoveAsync_CachedValue_IsComputedAgain()
    {
        var key = Key();
        await using var services = Compose(redis.ConnectionString);
        var cache = services.GetRequiredService<ICacheStore>();
        var calls = 0;

        await cache.GetOrCreateAsync(key, _ => ValueTask.FromResult(++calls), _hour, tags: null, CancellationToken);
        await cache.RemoveAsync(key, CancellationToken);

        (await cache.GetOrCreateAsync(key, _ => ValueTask.FromResult(++calls), _hour, tags: null, CancellationToken)).ShouldBe(2);
    }

    [Fact]
    public async Task Readiness_RedisRunning_IsHealthy()
    {
        await using var services = Compose(redis.ConnectionString);

        var report = await services.GetRequiredService<HealthCheckService>().CheckHealthAsync(CancellationToken);

        report.Entries["redis"].Status.ShouldBe(HealthStatus.Healthy);
    }

    private static string Key() => $"test:{Guid.CreateVersion7():N}";

    // HybridCache writes the distributed tier in the background, so a second instance only sees the value a moment later.
    private static async Task WaitUntilStoredInRedisAsync(IServiceProvider services, string key)
    {
        var distributed = services.GetRequiredService<IDistributedCache>();

        for (var attempt = 0; attempt < 50 && await distributed.GetAsync(key, CancellationToken) is null; attempt++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), CancellationToken);
        }
    }

    private static ServiceProvider Compose(string? redisConnection)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(redisConnection is null ? [] : [new KeyValuePair<string, string?>("ConnectionStrings:Redis", redisConnection)])
            .Build();

        return new ServiceCollection().AddLogging().AddCachingAdapter(configuration).BuildServiceProvider();
    }
}
