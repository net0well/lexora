using System.Runtime.CompilerServices;
using Lexora.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.Storage.Local;

public sealed class LocalFileStorage(
    IOptions<LocalStorageOptions> localOptions,
    IOptions<StorageOptions> storageOptions,
    LocalDownloadTickets tickets,
    TimeProvider timeProvider) : IFileStorage
{
    private const int BufferSize = 81920;

    private readonly string _root = Path.GetFullPath(localOptions.Value.RootPath);

    public async Task<StoredFile> SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        var path = PathOf(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using (var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        return new StoredFile(key, new FileInfo(path).Length);
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        var path = PathOf(key);

        return File.Exists(path)
            ? Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true))
            : throw new FileNotFoundException($"No stored file under '{key}'.", key);
    }

    public Task DeleteByPrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        var start = PathOf(prefix);
        var directory = prefix.EndsWith('/') ? start : Path.GetDirectoryName(start)!;

        if (!Directory.Exists(directory))
        {
            return Task.CompletedTask;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            if (file.StartsWith(start, StringComparison.Ordinal))
            {
                File.Delete(file);
            }
        }

        RemoveEmptyDirectories(directory);

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<StoredObject> ListAsync(string prefix, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var start = PathOf(prefix);
        var directory = prefix.EndsWith('/') ? start : Path.GetDirectoryName(start)!;

        if (!Directory.Exists(directory))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (file.StartsWith(start, StringComparison.Ordinal))
            {
                var key = Path.GetRelativePath(_root, file).Replace(Path.DirectorySeparatorChar, '/');

                yield return new StoredObject(key, new DateTimeOffset(File.GetLastWriteTimeUtc(file), TimeSpan.Zero));
            }
        }

        await Task.CompletedTask;
    }

    public Task<FileDownload> CreateDownloadAsync(string key, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var expiresAt = timeProvider.GetUtcNow() + storageOptions.Value.DownloadUrlLifetime;
        var ticket = tickets.Issue(new LocalDownloadTicket(key, fileName, contentType), expiresAt);
        var url = new Uri(localOptions.Value.DownloadBaseUrl!, Uri.EscapeDataString(ticket));

        return Task.FromResult(new FileDownload(url, expiresAt));
    }

    private string PathOf(string key)
    {
        var path = Path.GetFullPath(Path.Combine(_root, key));

        return path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            ? path
            : throw new ArgumentException($"The key '{key}' points outside the storage root.", nameof(key));
    }

    private void RemoveEmptyDirectories(string directory)
    {
        foreach (var child in Directory.EnumerateDirectories(directory))
        {
            RemoveEmptyDirectories(child);
        }

        if (directory != _root && !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
        }
    }
}
