using Lexora.Domain.Documents.Events;

namespace Lexora.Application.Ingestion.EmbedDocumentChunks;

public static class DocumentChunkedHandler
{
    public static EmbedPendingChunks Handle(DocumentChunked message) => new(message.DocumentId, message.ProcessingVersion);
}
