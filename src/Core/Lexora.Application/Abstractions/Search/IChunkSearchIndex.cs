namespace Lexora.Application.Abstractions.Search;

public interface IChunkSearchIndex
{
    Task<IReadOnlyList<ChunkSearchCandidate>> VectorSearchAsync(
        Guid knowledgeBaseId,
        float[] queryEmbedding,
        string embeddingSpace,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ChunkSearchCandidate>> KeywordSearchAsync(Guid knowledgeBaseId, string queryText, int limit, CancellationToken cancellationToken);
}
