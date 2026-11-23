using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Lexora.Application.Abstractions.Storage;

namespace Lexora.TestKit.Fakes;

public sealed class InMemoryFileStorage(TimeProvider timeProvider) : IFileStorage
{
    public static readonly TimeSpan DownloadLifetime = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, (byte[] Content, string ContentType)> _files = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _modified = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> Keys => [.. _files.Keys];

    public string ContentTypeOf(string key) => _files[key].ContentType;

    public byte[] ContentOf(string key) => _files[key].Content;

    public async Task<StoredFile> SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        _files[key] = (buffer.ToArray(), contentType);
        _modified[key] = timeProvider.GetUtcNow();

        return new StoredFile(key, buffer.Length);
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken) =>
        _files.TryGetValue(key, out var file)
            ? Task.FromResult<Stream>(new MemoryStream(file.Content, writable: false))
            : throw new FileNotFoundException($"No stored file under '{key}'.", key);

    public Task DeleteByPrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        foreach (var key in _files.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)))
        {
            _files.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<StoredObject> ListAsync(string prefix, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var key in _files.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal))
        {
            yield return new StoredObject(key, _modified.GetValueOrDefault(key, timeProvider.GetUtcNow()));
        }

        await Task.CompletedTask;
    }

    public void SetLastModified(string key, DateTimeOffset modified) => _modified[key] = modified;

    public Task<FileDownload> CreateDownloadAsync(string key, string fileName, string contentType, CancellationToken cancellationToken) =>
        Task.FromResult(new FileDownload(
            new Uri($"https://storage.test/{key}?name={Uri.EscapeDataString(fileName)}"),
            timeProvider.GetUtcNow() + DownloadLifetime));
}
