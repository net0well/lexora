namespace Lexora.Api.Contracts.Requests;

public sealed record StartConversationRequest(Guid KnowledgeBaseId, string? Title);
