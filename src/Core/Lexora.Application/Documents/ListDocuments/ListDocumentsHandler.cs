using Lexora.Application.Common.Paging;
using Lexora.Application.Documents.Abstractions;
using Lexora.Application.KnowledgeBases;
using Lexora.Shared.Results;

namespace Lexora.Application.Documents.ListDocuments;

public static class ListDocumentsHandler
{
    public static async Task<Result<PagedResponse<DocumentListItemResponse>>> Handle(
        ListDocumentsQuery query,
        IDocumentQueries queries,
        CancellationToken cancellationToken)
    {
        var page = await queries.ListAsync(query, cancellationToken);

        return page is null ? Result.Failure<PagedResponse<DocumentListItemResponse>>(KnowledgeBaseNotifications.NotFound) : page;
    }
}
