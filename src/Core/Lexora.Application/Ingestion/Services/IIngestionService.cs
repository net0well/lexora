namespace Lexora.Application.Ingestion.Services;

public interface IIngestionService
{
    Task ExtractAsync(Guid documentId, int processingVersion, CancellationToken cancellationToken);

    Task ChunkAsync(Guid documentId, int processingVersion, CancellationToken cancellationToken);
}
