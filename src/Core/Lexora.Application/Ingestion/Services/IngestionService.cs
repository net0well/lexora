using Lexora.Application.Abstractions.Processing;
using Lexora.Application.Abstractions.Storage;
using Lexora.Application.Documents;
using Lexora.Domain.Documents;
using Lexora.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Lexora.Application.Ingestion.Services;

public sealed class IngestionService(
    IDocumentRepository documents,
    IDocumentTextExtractor extractor,
    IFileStorage storage,
    TimeProvider timeProvider,
    ILogger<IngestionService> logger) : IIngestionService
{
    private const string ExtractionStage = "extraction";

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
