using Lexora.Application.Abstractions.Identity;
using Lexora.Application.Common.Paging;
using Lexora.Application.Conversations.Abstractions;
using Lexora.Shared.Results;

namespace Lexora.Application.Conversations.ListConversations;

public static class ListConversationsHandler
{
    public static async Task<Result<PagedResponse<ConversationListItemResponse>>> Handle(
        ListConversationsQuery query,
        IConversationQueries queries,
        ICurrentUser currentUser,
        CancellationToken cancellationToken) =>
        await queries.ListAsync(query, currentUser.UserId, cancellationToken);
}
