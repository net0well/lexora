using Lexora.Application.Common.Paging;
using Lexora.Application.Documents.GetDocument;
using Lexora.Application.Documents.ListDocumentChunks;
using Lexora.Application.Documents.ListDocuments;

namespace Lexora.Application.Documents.Abstractions;

public interface IDocumentQueries
{
    Task<GetDocumentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResponse<DocumentListItemResponse>?> ListAsync(ListDocumentsQuery query, CancellationToken cancellationToken);

    Task<PagedResponse<DocumentChunkResponse>?> ListChunksAsync(ListDocumentChunksQuery query, CancellationToken cancellationToken);

    Task<StoredDocumentFile?> GetStoredFileAsync(Guid id, CancellationToken cancellationToken);
}
