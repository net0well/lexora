namespace Lexora.Application.Ingestion.Services;

public interface IMaintenanceService
{
    Task RemoveDocumentDataAsync(Guid knowledgeBaseId, Guid documentId, CancellationToken cancellationToken);

    Task<int> SweepStalledDocumentsAsync(CancellationToken cancellationToken);

    Task<int> SweepOrphanFilesAsync(CancellationToken cancellationToken);

    Task<int> SweepExpiredRefreshTokensAsync(CancellationToken cancellationToken);
}
