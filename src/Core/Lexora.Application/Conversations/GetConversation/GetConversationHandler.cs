using Lexora.Application.Abstractions.Identity;
using Lexora.Application.Conversations.Abstractions;
using Lexora.Shared.Results;

namespace Lexora.Application.Conversations.GetConversation;

public static class GetConversationHandler
{
    public static async Task<Result<GetConversationResponse>> Handle(
        GetConversationQuery query,
        IConversationQueries queries,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var conversation = await queries.GetAsync(query.Id, currentUser.UserId, cancellationToken);

        return conversation is null ? Result.Failure<GetConversationResponse>(ConversationNotifications.NotFound) : conversation;
    }
}
