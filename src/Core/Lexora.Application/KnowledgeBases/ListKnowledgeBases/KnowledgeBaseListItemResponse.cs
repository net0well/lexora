using Lexora.Domain.KnowledgeBases;

namespace Lexora.Application.KnowledgeBases.ListKnowledgeBases;

public sealed record KnowledgeBaseListItemResponse(
    Guid Id,
    string Name,
    string? Description,
    KnowledgeBaseStatus Status,
    int DocumentCount,
    DateTimeOffset UpdatedAt);
