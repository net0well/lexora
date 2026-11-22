using Lexora.Application.Documents.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.Documents.ReprocessDocument;

public static class ReprocessDocumentHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result<ReprocessDocumentResponse>> Handle(ReprocessDocumentCommand command, IDocumentService service, CancellationToken cancellationToken) =>
        service.ReprocessAsync(command, cancellationToken);
}
