namespace Lexora.Application.Abstractions.Caching;

public interface ICacheStore
{
    ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan expiration,
        IReadOnlyCollection<string>? tags,
        CancellationToken cancellationToken);

    ValueTask RemoveAsync(string key, CancellationToken cancellationToken);

    ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken);
}
