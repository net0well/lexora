using Lexora.Domain.KnowledgeBases;

namespace Lexora.Application.KnowledgeBases.GetKnowledgeBase;

public sealed record GetKnowledgeBaseResponse(
    Guid Id,
    string Name,
    string? Description,
    KnowledgeBaseStatus Status,
    int ChunkMaxTokens,
    int ChunkOverlapTokens,
    int DocumentCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
