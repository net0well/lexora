using System.Collections.Concurrent;
using Lexora.Application.Abstractions.Caching;

namespace Lexora.TestKit.Fakes;

public sealed class InMemoryCacheStore : ICacheStore
{
    private readonly ConcurrentDictionary<string, object?> _entries = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> Keys => [.. _entries.Keys];

    public async ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan expiration,
        IReadOnlyCollection<string>? tags,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (_entries.TryGetValue(key, out var cached))
        {
            return (T)cached!;
        }

        var value = await factory(cancellationToken);
        _entries[key] = value;

        return value;
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken)
    {
        _entries.TryRemove(key, out _);

        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
