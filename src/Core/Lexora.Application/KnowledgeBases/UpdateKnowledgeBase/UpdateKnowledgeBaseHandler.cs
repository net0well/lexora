using Lexora.Application.KnowledgeBases.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.KnowledgeBases.UpdateKnowledgeBase;

public static class UpdateKnowledgeBaseHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result<UpdateKnowledgeBaseResponse>> Handle(
        UpdateKnowledgeBaseCommand command,
        IKnowledgeBaseService service,
        CancellationToken cancellationToken) =>
        service.UpdateAsync(command, cancellationToken);
}
