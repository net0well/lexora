using System.Collections.Concurrent;
using Lexora.Application.Abstractions.Storage;

namespace Lexora.TestKit.Fakes;

public sealed class InMemoryFileStorage(TimeProvider timeProvider) : IFileStorage
{
    public static readonly TimeSpan DownloadLifetime = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, (byte[] Content, string ContentType)> _files = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> Keys => [.. _files.Keys];

    public string ContentTypeOf(string key) => _files[key].ContentType;

    public byte[] ContentOf(string key) => _files[key].Content;

    public async Task<StoredFile> SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        _files[key] = (buffer.ToArray(), contentType);

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

    public Task<FileDownload> CreateDownloadAsync(string key, string fileName, string contentType, CancellationToken cancellationToken) =>
        Task.FromResult(new FileDownload(
            new Uri($"https://storage.test/{key}?name={Uri.EscapeDataString(fileName)}"),
            timeProvider.GetUtcNow() + DownloadLifetime));
}
