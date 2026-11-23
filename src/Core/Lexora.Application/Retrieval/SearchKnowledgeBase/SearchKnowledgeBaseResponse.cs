namespace Lexora.Application.Retrieval.SearchKnowledgeBase;

public sealed record SearchKnowledgeBaseResponse(IReadOnlyList<SearchHit> Hits);
