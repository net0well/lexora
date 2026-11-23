using Lexora.Application.Common.Options;
using Lexora.Application.KnowledgeBases;
using Lexora.Application.Retrieval.Services;
using Lexora.Domain.KnowledgeBases;
using Lexora.Shared.Results;
using Microsoft.Extensions.Options;

namespace Lexora.Application.Retrieval.SearchKnowledgeBase;

public static class SearchKnowledgeBaseHandler
{
    public static async Task<Result<SearchKnowledgeBaseResponse>> Handle(
        SearchKnowledgeBaseQuery query,
        IKnowledgeBaseRepository knowledgeBases,
        IRetrievalService retrieval,
        IOptions<RagOptions> options,
        CancellationToken cancellationToken)
    {
        if (await knowledgeBases.GetByIdAsync(query.KnowledgeBaseId, cancellationToken) is null)
        {
            return Result.Failure<SearchKnowledgeBaseResponse>(KnowledgeBaseNotifications.NotFound);
        }

        var hits = await retrieval.RetrieveAsync(
            query.KnowledgeBaseId,
            query.Query.Trim(),
            query.Mode ?? SearchMode.Hybrid,
            query.TopK ?? options.Value.TopK,
            cancellationToken);

        return new SearchKnowledgeBaseResponse(hits);
    }
}
