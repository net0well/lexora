namespace Lexora.Application.Retrieval.Services;

public interface IRetrievalService
{
    Task<IReadOnlyList<SearchHit>> RetrieveAsync(Guid knowledgeBaseId, string query, SearchMode mode, int topK, CancellationToken cancellationToken);
}
