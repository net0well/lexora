namespace Lexora.Application.KnowledgeBases.CreateKnowledgeBase;

public sealed record CreateKnowledgeBaseResponse(
    Guid Id,
    string Name,
    string? Description,
    int ChunkMaxTokens,
    int ChunkOverlapTokens,
    DateTimeOffset CreatedAt);
