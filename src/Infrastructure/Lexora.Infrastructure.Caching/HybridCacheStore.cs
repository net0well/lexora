using Lexora.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.Caching;

public sealed class HybridCacheStore(HybridCache cache, IOptions<CachingOptions> options) : ICacheStore
{
    public ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan expiration,
        IReadOnlyCollection<string>? tags,
        CancellationToken cancellationToken)
    {
        var entry = new HybridCacheEntryOptions
        {
            Expiration = expiration,
            LocalCacheExpiration = expiration < options.Value.LocalExpiration ? expiration : options.Value.LocalExpiration,
        };

        return cache.GetOrCreateAsync(key, factory, entry, tags, cancellationToken);
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken) => cache.RemoveAsync(key, cancellationToken);

    public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken) => cache.RemoveByTagAsync(tag, cancellationToken);
}
