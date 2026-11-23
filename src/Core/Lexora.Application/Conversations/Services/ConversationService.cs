using Lexora.Application.Abstractions.Identity;
using Lexora.Application.Conversations.StartConversation;
using Lexora.Application.KnowledgeBases;
using Lexora.Domain.Conversations;
using Lexora.Domain.KnowledgeBases;
using Lexora.Shared.Results;

namespace Lexora.Application.Conversations.Services;

public sealed class ConversationService(
    IConversationRepository conversations,
    IKnowledgeBaseRepository knowledgeBases,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IConversationService
{
    public async Task<Result<StartConversationResponse>> StartAsync(StartConversationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await knowledgeBases.GetByIdAsync(command.KnowledgeBaseId, cancellationToken) is null)
        {
            return Result.Failure<StartConversationResponse>(KnowledgeBaseNotifications.NotFound);
        }

        var started = Conversation.Start(command.KnowledgeBaseId, currentUser.UserId, command.Title, timeProvider.GetUtcNow());

        if (started.IsFailure)
        {
            return Result.Failure<StartConversationResponse>(started.Notifications);
        }

        var conversation = started.Value;
        await conversations.AddAsync(conversation, cancellationToken);

        return new StartConversationResponse(conversation.Id, conversation.KnowledgeBaseId, conversation.Title, conversation.CreatedAt);
    }

    public async Task<Result> DeleteAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var conversation = await conversations.GetByIdAsync(conversationId, cancellationToken);

        // A conversation of another user answers like a missing one, so ids cannot be probed.
        if (conversation is null || !conversation.IsOwnedBy(currentUser.UserId))
        {
            return Result.Failure(ConversationNotifications.NotFound);
        }

        conversations.Remove(conversation);

        return Result.Success();
    }
}
