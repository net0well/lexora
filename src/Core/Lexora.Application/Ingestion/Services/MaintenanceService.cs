using Lexora.Application.Abstractions.Storage;
using Lexora.Application.Common.Options;
using Lexora.Application.Documents;
using Lexora.Domain.Chunks;
using Lexora.Domain.Documents;
using Lexora.Domain.RefreshTokens;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexora.Application.Ingestion.Services;

public sealed class MaintenanceService(
    IDocumentRepository documents,
    IDocumentChunkRepository chunks,
    IRefreshTokenRepository refreshTokens,
    IFileStorage storage,
    IOptions<IngestionOptions> options,
    TimeProvider timeProvider,
    ILogger<MaintenanceService> logger) : IMaintenanceService
{
    public const string StalledReason = "Processing timed out.";
    public const int StalledBatchSize = 100;
    public const int OrphanCheckBatchSize = 500;

    public static readonly TimeSpan OrphanFileAge = TimeSpan.FromHours(24);
    public static readonly TimeSpan RefreshTokenRetention = TimeSpan.FromDays(30);

    private const string StoragePrefix = "knowledge-bases/";

    public async Task RemoveDocumentDataAsync(Guid knowledgeBaseId, Guid documentId, CancellationToken cancellationToken)
    {
        await storage.DeleteByPrefixAsync(DocumentStorageKeys.PrefixOf(knowledgeBaseId, documentId), cancellationToken);
        await chunks.DeleteByDocumentAsync(documentId, cancellationToken);
        logger.DocumentDataRemoved(documentId);
    }

    public async Task<int> SweepStalledDocumentsAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var stalled = await documents.ListStalledAsync(now - options.Value.StalledAfter, StalledBatchSize, cancellationToken);

        foreach (var document in stalled)
        {
            document.Fail(document.ProcessingVersion, StalledReason, now);
        }

        if (stalled.Count > 0)
        {
            logger.StalledDocumentsFailed(stalled.Count);
        }

        return stalled.Count;
    }

    public async Task<int> SweepOrphanFilesAsync(CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow() - OrphanFileAge;
        Dictionary<Guid, string> prefixes = [];

        await foreach (var stored in storage.ListAsync(StoragePrefix, cancellationToken))
        {
            if (stored.LastModified <= cutoff && TryParseDocumentKey(stored.Key, out var knowledgeBaseId, out var documentId))
            {
                prefixes.TryAdd(documentId, DocumentStorageKeys.PrefixOf(knowledgeBaseId, documentId));
            }
        }

        var removed = 0;

        foreach (var batch in prefixes.Keys.Chunk(OrphanCheckBatchSize))
        {
            var active = await documents.FindActiveIdsAsync(batch, cancellationToken);

            foreach (var documentId in batch.Where(id => !active.Contains(id)))
            {
                await storage.DeleteByPrefixAsync(prefixes[documentId], cancellationToken);
                removed++;
            }
        }

        if (removed > 0)
        {
            logger.OrphanFilesRemoved(removed);
        }

        return removed;
    }

    public async Task<int> SweepExpiredRefreshTokensAsync(CancellationToken cancellationToken)
    {
        var deleted = await refreshTokens.DeleteFinishedBeforeAsync(timeProvider.GetUtcNow() - RefreshTokenRetention, cancellationToken);

        if (deleted > 0)
        {
            logger.ExpiredRefreshTokensDeleted(deleted);
        }

        return deleted;
    }

    private static bool TryParseDocumentKey(string key, out Guid knowledgeBaseId, out Guid documentId)
    {
        var parts = key.Split('/');
        knowledgeBaseId = Guid.Empty;
        documentId = Guid.Empty;

        return parts.Length >= 5
            && parts[0] == "knowledge-bases"
            && parts[2] == "documents"
            && Guid.TryParse(parts[1], out knowledgeBaseId)
            && Guid.TryParse(parts[3], out documentId);
    }
}
