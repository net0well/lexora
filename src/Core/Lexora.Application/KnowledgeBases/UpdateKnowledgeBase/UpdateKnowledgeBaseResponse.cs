namespace Lexora.Application.KnowledgeBases.UpdateKnowledgeBase;

public sealed record UpdateKnowledgeBaseResponse(
    Guid Id,
    string Name,
    string? Description,
    int ChunkMaxTokens,
    int ChunkOverlapTokens,
    DateTimeOffset UpdatedAt);
