using Microsoft.Extensions.Logging;

namespace Lexora.Application.Ingestion.Services;

public static partial class IngestionLog
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Debug, Message = "Skipping stale {Stage} message for document {DocumentId} version {ProcessingVersion}")]
    public static partial void StaleMessageSkipped(this ILogger logger, string stage, Guid documentId, int processingVersion);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Extracted {PageCount} pages from document {DocumentId} version {ProcessingVersion}")]
    public static partial void TextExtracted(this ILogger logger, Guid documentId, int processingVersion, int pageCount);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Split document {DocumentId} version {ProcessingVersion} into {ChunkCount} chunks")]
    public static partial void DocumentChunked(this ILogger logger, Guid documentId, int processingVersion, int chunkCount);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Indexed document {DocumentId} version {ProcessingVersion} with {ChunkCount} chunks in {EmbeddingSpace}")]
    public static partial void DocumentIndexed(this ILogger logger, Guid documentId, int processingVersion, int chunkCount, string embeddingSpace);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Warning, Message = "Document {DocumentId} version {ProcessingVersion} failed: {Reason}")]
    public static partial void DocumentProcessingFailed(this ILogger logger, Guid documentId, int processingVersion, string reason);
}
