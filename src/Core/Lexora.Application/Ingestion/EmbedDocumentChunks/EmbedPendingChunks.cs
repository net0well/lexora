namespace Lexora.Application.Ingestion.EmbedDocumentChunks;

public sealed record EmbedPendingChunks(Guid DocumentId, int ProcessingVersion);
