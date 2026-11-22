using Lexora.Application.Common.Paging;
using Lexora.Application.KnowledgeBases.GetKnowledgeBase;
using Lexora.Application.KnowledgeBases.ListKnowledgeBases;

namespace Lexora.Application.KnowledgeBases.Abstractions;

public interface IKnowledgeBaseQueries
{
    Task<GetKnowledgeBaseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResponse<KnowledgeBaseListItemResponse>> ListAsync(ListKnowledgeBasesQuery query, CancellationToken cancellationToken);
}
