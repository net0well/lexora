using Lexora.Application.Documents.Abstractions;
using Lexora.Application.Ingestion.Services;
using Lexora.Domain.Documents.Events;
using Microsoft.Extensions.Logging;

namespace Lexora.Application.Ingestion.CompleteIndexing;

public sealed class DocumentIndexedHandler(IDocumentQueries queries, ILogger<DocumentIndexedHandler> logger)
{
    public async Task Handle(DocumentIndexed message, CancellationToken cancellationToken)
    {
        var document = await queries.GetByIdAsync(message.DocumentId, cancellationToken);

        if (document is not null)
        {
            logger.DocumentIndexed(document.Id, message.ProcessingVersion, document.ChunkCount, message.OccurredAt - document.UploadedAt);
        }
    }
}
