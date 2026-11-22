namespace Lexora.Domain.Chunks;

public interface IDocumentChunkRepository
{
    Task AddRangeAsync(IReadOnlyCollection<DocumentChunk> chunks, CancellationToken cancellationToken);

    Task<IReadOnlyList<DocumentChunk>> GetPendingEmbeddingAsync(
        Guid documentId,
        int processingVersion,
        int batchSize,
        CancellationToken cancellationToken);

    Task<int> DeleteOtherVersionsAsync(Guid documentId, int keepVersion, CancellationToken cancellationToken);
}
