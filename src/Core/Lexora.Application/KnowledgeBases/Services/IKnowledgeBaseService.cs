using Lexora.Application.KnowledgeBases.ArchiveKnowledgeBase;
using Lexora.Application.KnowledgeBases.CreateKnowledgeBase;
using Lexora.Application.KnowledgeBases.UpdateKnowledgeBase;
using Lexora.Shared.Results;

namespace Lexora.Application.KnowledgeBases.Services;

public interface IKnowledgeBaseService
{
    Task<Result<CreateKnowledgeBaseResponse>> CreateAsync(CreateKnowledgeBaseCommand command, CancellationToken cancellationToken);

    Task<Result<UpdateKnowledgeBaseResponse>> UpdateAsync(UpdateKnowledgeBaseCommand command, CancellationToken cancellationToken);

    Task<Result> ArchiveAsync(ArchiveKnowledgeBaseCommand command, CancellationToken cancellationToken);
}
