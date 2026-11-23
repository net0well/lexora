using Lexora.Application.Retrieval;

namespace Lexora.Application.Abstractions.Ai;

public interface IReranker
{
    Task<IReadOnlyList<SearchHit>> RerankAsync(string query, IReadOnlyList<SearchHit> hits, int top, CancellationToken cancellationToken);
}
