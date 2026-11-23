using Lexora.Application.Ingestion.Services;
using Lexora.Domain.Abstractions;
using Wolverine.Attributes;

namespace Lexora.Application.Ingestion.EmbedDocumentChunks;

public static class EmbedPendingChunksHandler
{
    // A local model on CPU can need minutes for a slow batch; the default limit of one minute would
    // cancel work that is still progressing and send the message to the error queue.
    [MessageTimeout(300)]
    [Transactional(typeof(IUnitOfWork))]
    public static Task<EmbedPendingChunks?> Handle(EmbedPendingChunks message, IIngestionService service, CancellationToken cancellationToken) =>
        service.EmbedNextBatchAsync(message, cancellationToken);
}
