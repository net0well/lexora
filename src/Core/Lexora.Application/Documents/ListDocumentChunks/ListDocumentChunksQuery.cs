using Lexora.Application.Common.Paging;

namespace Lexora.Application.Documents.ListDocumentChunks;

public sealed record ListDocumentChunksQuery(Guid DocumentId, int Page = PageRequest.DefaultPage, int PageSize = PageRequest.DefaultPageSize)
{
    public PageRequest Paging => new(Page, PageSize);
}
