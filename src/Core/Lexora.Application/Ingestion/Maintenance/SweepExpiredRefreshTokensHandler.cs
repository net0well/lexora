using Lexora.Application.Ingestion.Services;
using Lexora.Domain.Abstractions;
using Wolverine.Attributes;
using Wolverine.Persistence;

namespace Lexora.Application.Ingestion.Maintenance;

public static class SweepExpiredRefreshTokensHandler
{
    [Transactional(typeof(IUnitOfWork), Mode = TransactionMiddlewareMode.Eager)]
    public static Task Handle(SweepExpiredRefreshTokens message, IMaintenanceService service, CancellationToken cancellationToken) =>
        service.SweepExpiredRefreshTokensAsync(cancellationToken);
}
