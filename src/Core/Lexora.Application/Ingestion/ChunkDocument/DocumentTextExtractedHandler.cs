using Lexora.Application.Ingestion.Services;
using Lexora.Domain.Abstractions;
using Lexora.Domain.Documents.Events;
using Wolverine.Attributes;
using Wolverine.Persistence;

namespace Lexora.Application.Ingestion.ChunkDocument;

public static class DocumentTextExtractedHandler
{
    // Chunks of other versions are removed with a bulk delete that runs outside SaveChanges,
    // so the handler needs a real transaction to keep that delete and the new chunks together.
    [Transactional(typeof(IUnitOfWork), Mode = TransactionMiddlewareMode.Eager)]
    public static Task Handle(DocumentTextExtracted message, IIngestionService service, CancellationToken cancellationToken) =>
        service.ChunkAsync(message.DocumentId, message.ProcessingVersion, cancellationToken);
}
