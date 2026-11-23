using Lexora.Application.Ingestion.Services;
using Lexora.Domain.Abstractions;
using Wolverine.Attributes;

namespace Lexora.Application.Ingestion.EmbedDocumentChunks;

public static class EmbedPendingChunksHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task<EmbedPendingChunks?> Handle(EmbedPendingChunks message, IIngestionService service, CancellationToken cancellationToken) =>
        service.EmbedNextBatchAsync(message, cancellationToken);
}
