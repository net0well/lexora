using Lexora.Domain.Chunks;
using Lexora.Shared.Guards;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Persistence.Repositories;

public sealed class DocumentChunkRepository(LexoraDbContext context) : IDocumentChunkRepository
{
    public Task AddRangeAsync(IReadOnlyCollection<DocumentChunk> chunks, CancellationToken cancellationToken) =>
        context.DocumentChunks.AddRangeAsync(chunks, cancellationToken);

    public async Task<IReadOnlyList<DocumentChunk>> GetPendingEmbeddingAsync(
        Guid documentId,
        int processingVersion,
        int batchSize,
        CancellationToken cancellationToken)
    {
        Guard.Positive(batchSize);

        return await context.DocumentChunks
            .Where(chunk => chunk.DocumentId == documentId && chunk.ProcessingVersion == processingVersion && chunk.Embedding == null)
            .OrderBy(chunk => chunk.Index)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> DeleteByDocumentAsync(Guid documentId, CancellationToken cancellationToken) =>
        context.DocumentChunks.Where(chunk => chunk.DocumentId == documentId).ExecuteDeleteAsync(cancellationToken);

    public Task<int> DeleteOtherVersionsAsync(Guid documentId, int keepVersion, CancellationToken cancellationToken) =>
        context.DocumentChunks
            .Where(chunk => chunk.DocumentId == documentId && chunk.ProcessingVersion != keepVersion)
            .ExecuteDeleteAsync(cancellationToken);
}
