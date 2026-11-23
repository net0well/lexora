namespace Lexora.Application.Conversations.StartConversation;

public sealed record StartConversationCommand(Guid KnowledgeBaseId, string? Title);
