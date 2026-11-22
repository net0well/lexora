using Lexora.Application.Ingestion.Services;
using Lexora.Domain.Abstractions;
using Lexora.Domain.Documents.Events;
using Wolverine.Attributes;

namespace Lexora.Application.Ingestion.ExtractDocumentText;

public static class DocumentUploadedHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task Handle(DocumentUploaded message, IIngestionService service, CancellationToken cancellationToken) =>
        service.ExtractAsync(message.DocumentId, message.ProcessingVersion, cancellationToken);
}
