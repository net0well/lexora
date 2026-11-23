using Lexora.Application.Ingestion.Services;

namespace Lexora.Application.Ingestion.Maintenance;

public static class SweepOrphanFilesHandler
{
    public static Task Handle(SweepOrphanFiles message, IMaintenanceService service, CancellationToken cancellationToken) =>
        service.SweepOrphanFilesAsync(cancellationToken);
}
