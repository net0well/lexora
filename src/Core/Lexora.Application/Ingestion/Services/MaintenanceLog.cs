using Microsoft.Extensions.Logging;

namespace Lexora.Application.Ingestion.Services;

public static partial class MaintenanceLog
{
    [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Removed stored files and chunks of deleted document {DocumentId}")]
    public static partial void DocumentDataRemoved(this ILogger logger, Guid documentId);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "Knowledge base {KnowledgeBaseId} was archived")]
    public static partial void KnowledgeBaseArchived(this ILogger logger, Guid knowledgeBaseId);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Warning, Message = "Marked {Count} stalled documents as failed")]
    public static partial void StalledDocumentsFailed(this ILogger logger, int count);

    [LoggerMessage(EventId = 3004, Level = LogLevel.Information, Message = "Removed stored files of {Count} documents that no longer exist")]
    public static partial void OrphanFilesRemoved(this ILogger logger, int count);

    [LoggerMessage(EventId = 3005, Level = LogLevel.Information, Message = "Deleted {Count} expired or revoked refresh tokens")]
    public static partial void ExpiredRefreshTokensDeleted(this ILogger logger, int count);
}
