using Lexora.Application.Common.Paging;

namespace Lexora.Application.Conversations.ListConversations;

public sealed record ListConversationsQuery(
    Guid? KnowledgeBaseId,
    int Page = PageRequest.DefaultPage,
    int PageSize = PageRequest.DefaultPageSize)
{
    public PageRequest Paging => new(Page, PageSize);
}
