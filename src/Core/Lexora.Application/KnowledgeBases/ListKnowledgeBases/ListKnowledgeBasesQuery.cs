using Lexora.Application.Common.Paging;
using Lexora.Domain.KnowledgeBases;

namespace Lexora.Application.KnowledgeBases.ListKnowledgeBases;

public sealed record ListKnowledgeBasesQuery(
    KnowledgeBaseStatus? Status,
    int Page = PageRequest.DefaultPage,
    int PageSize = PageRequest.DefaultPageSize)
{
    public PageRequest Paging => new(Page, PageSize);
}
