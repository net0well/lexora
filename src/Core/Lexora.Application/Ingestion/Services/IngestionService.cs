using Lexora.Application.Abstractions.Processing;
using Lexora.Application.Abstractions.Storage;
using Lexora.Application.Documents;
using Lexora.Domain.Chunks;
using Lexora.Domain.Documents;
using Lexora.Domain.KnowledgeBases;
using Lexora.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Lexora.Application.Ingestion.Services;

public sealed class IngestionService(
    IDocumentRepository documents,
    IKnowledgeBaseRepository knowledgeBases,
    IDocumentChunkRepository chunks,
    IDocumentTextExtractor extractor,
    ITextChunker chunker,
    IFileStorage storage,
    TimeProvider timeProvider,
    ILogger<IngestionService> logger) : IIngestionService
{
    private const string ExtractionStage = "extraction";
    private const string ChunkingStage = "chunking";

    public async Task ExtractAsync(Guid documentId, int processingVersion, CancellationToken cancellationToken)
    {
        var document = await documents.GetByIdAsync(documentId, cancellationToken);

        if (document is null || document.ProcessingVersion != processingVersion || document.Status != DocumentStatus.Uploaded)
        {
            logger.StaleMessageSkipped(ExtractionStage, documentId, processingVersion);
            return;
        }

        var extracted = await ExtractTextAsync(document, cancellationToken);

        if (extracted.IsFailure)
        {
            Fail(document, extracted.Notifications[0].Message);
            return;
        }

        var key = DocumentStorageKeys.ExtractedOf(document.KnowledgeBaseId, document.Id, processingVersion);

        await using (var json = await ExtractedDocumentJson.SerializeAsync(extracted.Value, cancellationToken))
        {
            await storage.SaveAsync(key, json, ExtractedDocumentJson.ContentType, cancellationToken);
        }

        document.CompleteExtraction(processingVersion, extracted.Value.PageCount, key, timeProvider.GetUtcNow());
        logger.TextExtracted(document.Id, processingVersion, extracted.Value.PageCount);
    }

    public async Task ChunkAsync(Guid documentId, int processingVersion, CancellationToken cancellationToken)
    {
        var document = await documents.GetByIdAsync(documentId, cancellationToken);

        if (document is null || document.ProcessingVersion != processingVersion || document.Status != DocumentStatus.Extracted)
        {
            logger.StaleMessageSkipped(ChunkingStage, documentId, processingVersion);
            return;
        }

        var knowledgeBase = await knowledgeBases.GetByIdAsync(document.KnowledgeBaseId, cancellationToken);
        var extracted = await ReadExtractedAsync(document, cancellationToken);

        if (knowledgeBase is null || extracted is null)
        {
            Fail(document, ExtractionFailures.Unreadable.Message);
            return;
        }

        var pieces = chunker.Chunk(extracted, knowledgeBase.Chunking);

        if (pieces.Count == 0)
        {
            Fail(document, ExtractionFailures.NoText.Message);
            return;
        }

        await chunks.DeleteOtherVersionsAsync(document.Id, processingVersion, cancellationToken);
        await chunks.AddRangeAsync(
            [
                .. pieces.Select(piece => DocumentChunk.Create(
                    document.KnowledgeBaseId,
                    document.Id,
                    processingVersion,
                    piece.Index,
                    piece.Content,
                    piece.TokenCount,
                    ChunkLocation.Create(piece.PageNumber, piece.SheetName, piece.SectionTitle, piece.StartOffset, piece.EndOffset))),
            ],
            cancellationToken);

        document.CompleteChunking(processingVersion, pieces.Count, timeProvider.GetUtcNow());
        logger.DocumentChunked(document.Id, processingVersion, pieces.Count);
    }

    private async Task<ExtractedDocument?> ReadExtractedAsync(Document document, CancellationToken cancellationToken)
    {
        if (document.ExtractedTextKey is not { } key)
        {
            return null;
        }

        try
        {
            await using var content = await storage.OpenReadAsync(key, cancellationToken);

            return await ExtractedDocumentJson.DeserializeAsync(content, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    private async Task<Result<ExtractedDocument>> ExtractTextAsync(Document document, CancellationToken cancellationToken)
    {
        try
        {
            await using var original = await storage.OpenReadAsync(document.StorageKey, cancellationToken);

            return await extractor.ExtractAsync(original, document.Format, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return Result.Failure<ExtractedDocument>(ExtractionFailures.Unreadable);
        }
    }

    private void Fail(Document document, string reason)
    {
        document.Fail(document.ProcessingVersion, reason, timeProvider.GetUtcNow());
        logger.DocumentProcessingFailed(document.Id, document.ProcessingVersion, reason);
    }
}
