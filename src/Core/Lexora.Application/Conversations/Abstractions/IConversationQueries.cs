using Lexora.Application.Common.Paging;
using Lexora.Application.Conversations.GetConversation;
using Lexora.Application.Conversations.ListConversations;

namespace Lexora.Application.Conversations.Abstractions;

public interface IConversationQueries
{
    Task<GetConversationResponse?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    Task<PagedResponse<ConversationListItemResponse>> ListAsync(ListConversationsQuery query, Guid userId, CancellationToken cancellationToken);
}
