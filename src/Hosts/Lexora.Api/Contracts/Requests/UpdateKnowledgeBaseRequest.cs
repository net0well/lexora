namespace Lexora.Api.Contracts.Requests;

public sealed record UpdateKnowledgeBaseRequest(string Name, string? Description, int? ChunkMaxTokens, int? ChunkOverlapTokens);
