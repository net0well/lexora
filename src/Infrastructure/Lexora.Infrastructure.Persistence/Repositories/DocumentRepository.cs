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

    public async Task AddAsync(Document document, CancellationToken cancellationToken) =>
        await context.Documents.AddAsync(document, cancellationToken);
}
