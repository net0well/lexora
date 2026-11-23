namespace Lexora.Application.Conversations.StartConversation;

public sealed record StartConversationResponse(Guid Id, Guid KnowledgeBaseId, string Title, DateTimeOffset CreatedAt);
