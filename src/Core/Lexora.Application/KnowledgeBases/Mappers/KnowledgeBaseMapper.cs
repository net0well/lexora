using Lexora.Application.KnowledgeBases.CreateKnowledgeBase;
using Lexora.Application.KnowledgeBases.UpdateKnowledgeBase;
using Lexora.Domain.KnowledgeBases;

namespace Lexora.Application.KnowledgeBases.Mappers;

public static class KnowledgeBaseMapper
{
    public static CreateKnowledgeBaseResponse ToCreateResponse(this KnowledgeBase knowledgeBase) => new(
        knowledgeBase.Id,
        knowledgeBase.Name.Value,
        knowledgeBase.Description,
        knowledgeBase.Chunking.MaxTokens,
        knowledgeBase.Chunking.OverlapTokens,
        knowledgeBase.CreatedAt);

    public static UpdateKnowledgeBaseResponse ToUpdateResponse(this KnowledgeBase knowledgeBase) => new(
        knowledgeBase.Id,
        knowledgeBase.Name.Value,
        knowledgeBase.Description,
        knowledgeBase.Chunking.MaxTokens,
        knowledgeBase.Chunking.OverlapTokens,
        knowledgeBase.UpdatedAt);
}
