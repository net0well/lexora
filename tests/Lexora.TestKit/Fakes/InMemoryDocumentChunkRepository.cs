using Lexora.Domain.Chunks;

namespace Lexora.TestKit.Fakes;

public sealed class InMemoryDocumentChunkRepository : IDocumentChunkRepository
{
    private readonly List<DocumentChunk> _chunks = [];

    public IReadOnlyList<DocumentChunk> Chunks => _chunks;

    public Task AddRangeAsync(IReadOnlyCollection<DocumentChunk> chunks, CancellationToken cancellationToken)
    {
        _chunks.AddRange(chunks);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DocumentChunk>> GetPendingEmbeddingAsync(Guid documentId, int processingVersion, int batchSize, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DocumentChunk>>(
        [
            .. _chunks
                .Where(chunk => chunk.DocumentId == documentId && chunk.ProcessingVersion == processingVersion && !chunk.HasEmbedding)
                .OrderBy(chunk => chunk.Index)
                .Take(batchSize),
        ]);

    public Task<int> DeleteOtherVersionsAsync(Guid documentId, int keepVersion, CancellationToken cancellationToken) =>
        Task.FromResult(_chunks.RemoveAll(chunk => chunk.DocumentId == documentId && chunk.ProcessingVersion != keepVersion));
}
