using Lexora.Application.Ingestion.Services;
using Lexora.Domain.Abstractions;
using Wolverine.Attributes;

namespace Lexora.Application.Ingestion.Maintenance;

public static class SweepStalledDocumentsHandler
{
    [Transactional(typeof(IUnitOfWork))]
    public static Task Handle(SweepStalledDocuments message, IMaintenanceService service, CancellationToken cancellationToken) =>
        service.SweepStalledDocumentsAsync(cancellationToken);
}
