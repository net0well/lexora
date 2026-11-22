using Lexora.Application.Documents.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.Documents.DeleteDocument;

public static class DeleteDocumentHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result> Handle(DeleteDocumentCommand command, IDocumentService service, CancellationToken cancellationToken) =>
        service.DeleteAsync(command, cancellationToken);
}
