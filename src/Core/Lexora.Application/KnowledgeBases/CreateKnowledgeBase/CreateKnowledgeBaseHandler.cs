using Lexora.Application.KnowledgeBases.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.KnowledgeBases.CreateKnowledgeBase;

public static class CreateKnowledgeBaseHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result<CreateKnowledgeBaseResponse>> Handle(
        CreateKnowledgeBaseCommand command,
        IKnowledgeBaseService service,
        CancellationToken cancellationToken) =>
        service.CreateAsync(command, cancellationToken);
}
