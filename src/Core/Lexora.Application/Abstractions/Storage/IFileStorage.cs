namespace Lexora.Application.Abstractions.Storage;

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken);

    Task DeleteByPrefixAsync(string prefix, CancellationToken cancellationToken);

    Task<FileDownload> CreateDownloadAsync(string key, string fileName, string contentType, CancellationToken cancellationToken);
}
