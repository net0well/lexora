using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Abstractions.Caching;
using Lexora.Application.Abstractions.Search;
using Lexora.Application.Common.Options;
using Lexora.Application.Retrieval.Fusion;
using Microsoft.Extensions.Options;

namespace Lexora.Application.Retrieval.Services;

public sealed class RetrievalService(
    IChunkSearchIndex index,
    IEmbeddingModel embeddings,
    ICacheStore cache,
    IRankFusion fusion,
    IReranker reranker,
    IOptions<RagOptions> options) : IRetrievalService
{
    public static readonly TimeSpan QueryEmbeddingLifetime = TimeSpan.FromHours(1);

    public async Task<IReadOnlyList<SearchHit>> RetrieveAsync(Guid knowledgeBaseId, string query, SearchMode mode, int topK, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var settings = options.Value;
        var vector = mode == SearchMode.Keyword ? [] : await VectorCandidatesAsync(knowledgeBaseId, query, settings, cancellationToken);
        var keyword = mode == SearchMode.Vector ? [] : await index.KeywordSearchAsync(knowledgeBaseId, query, settings.CandidatePoolSize, cancellationToken);

        var fused = fusion.Fuse(vector, keyword).Select(ToHit).ToList();
        var reranked = await reranker.RerankAsync(query, fused, fused.Count, cancellationToken);

        return Diverse(reranked, settings.MaxChunksPerDocument, topK);
    }

    private static List<SearchHit> Diverse(IReadOnlyList<SearchHit> ranked, int maxPerDocument, int topK)
    {
        Dictionary<Guid, int> perDocument = [];
        List<SearchHit> selected = [];

        foreach (var hit in ranked)
        {
            if (selected.Count == topK)
            {
                break;
            }

            var count = perDocument.GetValueOrDefault(hit.DocumentId);

            if (count < maxPerDocument)
            {
                perDocument[hit.DocumentId] = count + 1;
                selected.Add(hit);
            }
        }

        return selected;
    }

    private async Task<IReadOnlyList<ChunkSearchCandidate>> VectorCandidatesAsync(
        Guid knowledgeBaseId,
        string query,
        RagOptions settings,
        CancellationToken cancellationToken)
    {
        var embedding = await cache.GetOrCreateAsync(
            QueryEmbeddingCacheKey.For(embeddings.Space, query),
            async token => (await embeddings.EmbedAsync([query], EmbeddingPurpose.Query, token))[0],
            QueryEmbeddingLifetime,
            tags: null,
            cancellationToken);
        var candidates = await index.VectorSearchAsync(knowledgeBaseId, embedding, embeddings.Space.Key, settings.CandidatePoolSize, cancellationToken);

        return [.. candidates.Where(candidate => candidate.Score >= settings.MinVectorSimilarity)];
    }

    private static SearchHit ToHit(FusedCandidate fused) =>
        new(
            fused.Candidate.ChunkId,
            fused.Candidate.DocumentId,
            fused.Candidate.DocumentName,
            fused.Candidate.Content,
            fused.Candidate.PageNumber,
            fused.Candidate.SheetName,
            fused.Candidate.SectionTitle,
            fused.Score,
            fused.MatchedBy);
}
