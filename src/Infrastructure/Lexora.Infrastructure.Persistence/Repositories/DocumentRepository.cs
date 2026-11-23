using Lexora.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Persistence.Repositories;

public sealed class DocumentRepository(LexoraDbContext context) : IDocumentRepository
{
    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Documents.FirstOrDefaultAsync(document => document.Id == id && document.DeletedAt == null, cancellationToken);

    public Task<bool> ExistsWithHashAsync(Guid knowledgeBaseId, ContentHash hash, CancellationToken cancellationToken) =>
        context.Documents.AnyAsync(
            document => document.KnowledgeBaseId == knowledgeBaseId && document.ContentHash == hash && document.DeletedAt == null,
            cancellationToken);

    public Task<long> SumActiveSizeAsync(CancellationToken cancellationToken) =>
        context.Documents.Where(document => document.DeletedAt == null).SumAsync(document => document.SizeBytes, cancellationToken);

    public async Task<IReadOnlyList<Document>> ListIndexedAsync(Guid knowledgeBaseId, CancellationToken cancellationToken) =>
        await context.Documents
            .Where(document => document.KnowledgeBaseId == knowledgeBaseId && document.DeletedAt == null && document.Status == DocumentStatus.Indexed)
            .OrderBy(document => document.UploadedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Document>> ListStalledAsync(DateTimeOffset updatedBefore, int limit, CancellationToken cancellationToken) =>
        await context.Documents
            .Where(document => document.DeletedAt == null
                && (document.Status == DocumentStatus.Uploaded || document.Status == DocumentStatus.Extracted || document.Status == DocumentStatus.Chunked)
                && document.UpdatedAt < updatedBefore)
            .OrderBy(document => document.UpdatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<Guid>> FindActiveIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        (await context.Documents
            .Where(document => ids.Contains(document.Id) && document.DeletedAt == null)
            .Select(document => document.Id)
            .ToListAsync(cancellationToken))
        .ToHashSet();

    public async Task AddAsync(Document document, CancellationToken cancellationToken) =>
        await context.Documents.AddAsync(document, cancellationToken);
}
