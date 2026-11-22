namespace Lexora.Api.Contracts.Requests;

public sealed record CreateKnowledgeBaseRequest(string Name, string? Description, int? ChunkMaxTokens, int? ChunkOverlapTokens);
