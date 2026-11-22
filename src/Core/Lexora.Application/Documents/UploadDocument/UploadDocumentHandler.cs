using Lexora.Application.Documents.Services;
using Lexora.Domain.Abstractions;
using Lexora.Shared.Results;
using Wolverine.Attributes;

namespace Lexora.Application.Documents.UploadDocument;

public static class UploadDocumentHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<Result<UploadDocumentResponse>> Handle(UploadDocumentCommand command, IDocumentService service, CancellationToken cancellationToken) =>
        service.UploadAsync(command, cancellationToken);
}
