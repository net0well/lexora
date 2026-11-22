namespace Lexora.Application.KnowledgeBases.CreateKnowledgeBase;

public sealed record CreateKnowledgeBaseCommand(
    string Name,
    string? Description,
    int? ChunkMaxTokens,
    int? ChunkOverlapTokens);
