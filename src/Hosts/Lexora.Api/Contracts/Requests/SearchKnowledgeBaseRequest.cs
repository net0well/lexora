using Lexora.Application.Retrieval;

namespace Lexora.Api.Contracts.Requests;

public sealed record SearchKnowledgeBaseRequest(string? Query, SearchMode? Mode, int? TopK);
