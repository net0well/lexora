using Lexora.Application.Documents.Abstractions;
using Lexora.Application.Ingestion.Services;
using Lexora.Domain.Documents.Events;
using Microsoft.Extensions.Logging;

namespace Lexora.Application.Ingestion.CompleteIndexing;

public sealed class DocumentFailedHandler(IDocumentQueries queries, ILogger<DocumentFailedHandler> logger)
{
    public async Task Handle(DocumentFailed message, CancellationToken cancellationToken)
    {
        var document = await queries.GetByIdAsync(message.DocumentId, cancellationToken);

        if (document is not null)
        {
            logger.DocumentFailed(document.Id, message.ProcessingVersion, document.Format, document.FailureReason ?? "Unknown reason.");
        }
    }
}
