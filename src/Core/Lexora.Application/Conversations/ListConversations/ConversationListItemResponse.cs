namespace Lexora.Application.Conversations.ListConversations;

public sealed record ConversationListItemResponse(
    Guid Id,
    Guid KnowledgeBaseId,
    string Title,
    int MessageCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
