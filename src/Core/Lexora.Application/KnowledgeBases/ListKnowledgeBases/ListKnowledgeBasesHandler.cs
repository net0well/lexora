using Lexora.Application.Common.Paging;
using Lexora.Application.KnowledgeBases.Abstractions;
using Lexora.Shared.Results;

namespace Lexora.Application.KnowledgeBases.ListKnowledgeBases;

public static class ListKnowledgeBasesHandler
{
    public static async Task<Result<PagedResponse<KnowledgeBaseListItemResponse>>> Handle(
        ListKnowledgeBasesQuery query,
        IKnowledgeBaseQueries queries,
        CancellationToken cancellationToken) =>
        await queries.ListAsync(query, cancellationToken);
}
