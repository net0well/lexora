using Lexora.Application.Abstractions.Storage;
using Lexora.Application.Documents.Abstractions;
using Lexora.Shared.Results;

namespace Lexora.Application.Documents.GetDocumentDownload;

public static class GetDocumentDownloadHandler
{
    public static async Task<Result<DocumentDownloadResponse>> Handle(
        GetDocumentDownloadQuery query,
        IDocumentQueries queries,
        IFileStorage storage,
        CancellationToken cancellationToken)
    {
        var file = await queries.GetStoredFileAsync(query.Id, cancellationToken);

        if (file is null)
        {
            return Result.Failure<DocumentDownloadResponse>(DocumentNotifications.NotFound);
        }

        var download = await storage.CreateDownloadAsync(file.StorageKey, file.FileName, file.ContentType, cancellationToken);

        return new DocumentDownloadResponse(download.Url, download.ExpiresAt);
    }
}
