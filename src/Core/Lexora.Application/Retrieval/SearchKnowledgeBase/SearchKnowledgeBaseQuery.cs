namespace Lexora.Application.Retrieval.SearchKnowledgeBase;

public sealed record SearchKnowledgeBaseQuery(Guid KnowledgeBaseId, string Query, SearchMode? Mode = null, int? TopK = null);
