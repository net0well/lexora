using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Retrieval;

namespace Lexora.Infrastructure.AI.Adapters;

public sealed class NoOpReranker : IReranker
{
    public Task<IReadOnlyList<SearchHit>> RerankAsync(string query, IReadOnlyList<SearchHit> hits, int top, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(hits);

        return Task.FromResult<IReadOnlyList<SearchHit>>([.. hits.Take(top)]);
    }
}
