using Lexora.Application.Common.Paging;
using Lexora.Domain.Documents;

namespace Lexora.Application.Documents.ListDocuments;

public sealed record ListDocumentsQuery(
    Guid KnowledgeBaseId,
    DocumentStatus? Status,
    string? Search,
    int Page = PageRequest.DefaultPage,
    int PageSize = PageRequest.DefaultPageSize)
{
    public PageRequest Paging => new(Page, PageSize);
}
