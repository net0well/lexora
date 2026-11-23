namespace Lexora.Application.Conversations.GetConversation;

public sealed record GetConversationResponse(
    Guid Id,
    Guid KnowledgeBaseId,
    string Title,
    IReadOnlyList<ConversationMessageResponse> Messages,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
