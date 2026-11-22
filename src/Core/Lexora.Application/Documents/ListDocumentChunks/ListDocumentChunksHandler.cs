using Lexora.Application.Common.Paging;
using Lexora.Application.Documents.Abstractions;
using Lexora.Shared.Results;

namespace Lexora.Application.Documents.ListDocumentChunks;

public static class ListDocumentChunksHandler
{
    public static async Task<Result<PagedResponse<DocumentChunkResponse>>> Handle(
        ListDocumentChunksQuery query,
        IDocumentQueries queries,
        CancellationToken cancellationToken)
    {
        var page = await queries.ListChunksAsync(query, cancellationToken);

        return page is null ? Result.Failure<PagedResponse<DocumentChunkResponse>>(DocumentNotifications.NotFound) : page;
    }
}
