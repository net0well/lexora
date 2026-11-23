using Lexora.Application.KnowledgeBases.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.KnowledgeBases.ReindexKnowledgeBase;

public static class ReindexKnowledgeBaseHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result<ReindexKnowledgeBaseResponse>> Handle(
        ReindexKnowledgeBaseCommand command,
        IKnowledgeBaseService service,
        CancellationToken cancellationToken) =>
        service.ReindexAsync(command, cancellationToken);
}
