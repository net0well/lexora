using Lexora.Application.KnowledgeBases.Abstractions;
using Lexora.Shared.Results;

namespace Lexora.Application.KnowledgeBases.GetKnowledgeBase;

public static class GetKnowledgeBaseHandler
{
    public static async Task<Result<GetKnowledgeBaseResponse>> Handle(
        GetKnowledgeBaseQuery query,
        IKnowledgeBaseQueries queries,
        CancellationToken cancellationToken)
    {
        var response = await queries.GetByIdAsync(query.Id, cancellationToken);

        return response is null ? Result.Failure<GetKnowledgeBaseResponse>(KnowledgeBaseNotifications.NotFound) : response;
    }
}
