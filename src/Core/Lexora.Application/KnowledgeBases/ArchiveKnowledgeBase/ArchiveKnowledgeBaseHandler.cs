using Lexora.Application.KnowledgeBases.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.KnowledgeBases.ArchiveKnowledgeBase;

public static class ArchiveKnowledgeBaseHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result> Handle(ArchiveKnowledgeBaseCommand command, IKnowledgeBaseService service, CancellationToken cancellationToken) =>
        service.ArchiveAsync(command, cancellationToken);
}
