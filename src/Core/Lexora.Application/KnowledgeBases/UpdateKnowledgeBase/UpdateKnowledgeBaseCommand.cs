namespace Lexora.Application.KnowledgeBases.UpdateKnowledgeBase;

public sealed record UpdateKnowledgeBaseCommand(
    Guid Id,
    string Name,
    string? Description,
    int? ChunkMaxTokens,
    int? ChunkOverlapTokens);
