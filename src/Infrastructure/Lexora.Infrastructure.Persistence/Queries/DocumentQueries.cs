using Lexora.Application.Common.Paging;
using Lexora.Application.Documents.Abstractions;
using Lexora.Application.Documents.GetDocument;
using Lexora.Application.Documents.ListDocumentChunks;
using Lexora.Application.Documents.ListDocuments;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Persistence.Queries;

public sealed class DocumentQueries(LexoraDbContext context) : IDocumentQueries
{
    public Task<GetDocumentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Documents
            .AsNoTracking()
            .Where(document => document.Id == id && document.DeletedAt == null)
            .Select(document => new GetDocumentResponse(
                document.Id,
                document.KnowledgeBaseId,
                document.FileName.Value,
                document.Format,
                document.ContentType,
                document.SizeBytes,
                document.Status,
                document.ProcessingVersion,
                document.PageCount,
                document.ChunkCount,
                document.EmbeddedChunkCount,
                document.FailureReason,
                document.UploadedBy,
                document.UploadedAt,
                document.IndexedAt,
                document.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResponse<DocumentListItemResponse>?> ListAsync(ListDocumentsQuery query, CancellationToken cancellationToken)
    {
        if (!await context.KnowledgeBases.AnyAsync(knowledgeBase => knowledgeBase.Id == query.KnowledgeBaseId, cancellationToken))
        {
            return null;
        }

        var documents = context.Documents
            .AsNoTracking()
            .Where(document => document.KnowledgeBaseId == query.KnowledgeBaseId && document.DeletedAt == null);

        if (query.Status is { } status)
        {
            documents = documents.Where(document => document.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = LikePattern.Contains(query.Search.Trim());

            // FileName uses a value converter; the object cast makes EF compare the stored text column.
            documents = documents.Where(document => EF.Functions.ILike((string)(object)document.FileName, pattern, LikePattern.EscapeCharacter));
        }

        var paging = query.Paging;
        var totalItems = await documents.CountAsync(cancellationToken);
        var items = await documents
            .OrderByDescending(document => document.UploadedAt)
            .ThenByDescending(document => document.Id)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(document => new DocumentListItemResponse(
                document.Id,
                document.FileName.Value,
                document.Format,
                document.SizeBytes,
                document.Status,
                document.ChunkCount,
                document.EmbeddedChunkCount,
                document.UploadedAt,
                document.UpdatedAt))
            .ToListAsync(cancellationToken);

        return PagedResponse.Create<DocumentListItemResponse>(items, paging, totalItems);
    }

    public async Task<PagedResponse<DocumentChunkResponse>?> ListChunksAsync(ListDocumentChunksQuery query, CancellationToken cancellationToken)
    {
        var version = await context.Documents
            .AsNoTracking()
            .Where(document => document.Id == query.DocumentId && document.DeletedAt == null)
            .Select(document => (int?)document.ProcessingVersion)
            .FirstOrDefaultAsync(cancellationToken);

        if (version is null)
        {
            return null;
        }

        var chunks = context.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunk.DocumentId == query.DocumentId && chunk.ProcessingVersion == version);

        var paging = query.Paging;
        var totalItems = await chunks.CountAsync(cancellationToken);
        var items = await chunks
            .OrderBy(chunk => chunk.Index)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(chunk => new DocumentChunkResponse(
                chunk.Id,
                chunk.Index,
                chunk.Content,
                chunk.TokenCount,
                chunk.Location.PageNumber,
                chunk.Location.SheetName,
                chunk.Location.SectionTitle))
            .ToListAsync(cancellationToken);

        return PagedResponse.Create<DocumentChunkResponse>(items, paging, totalItems);
    }
}
