using Lexora.Application.Ingestion.Services;
using Lexora.Domain.Abstractions;
using Lexora.Domain.Documents.Events;
using Wolverine.Attributes;
using Wolverine.Persistence;

namespace Lexora.Application.Ingestion.Maintenance;

public static class DocumentDeletedHandler
{
    [Transactional(typeof(IUnitOfWork), Mode = TransactionMiddlewareMode.Eager)]
    public static Task Handle(DocumentDeleted message, IMaintenanceService service, CancellationToken cancellationToken) =>
        service.RemoveDocumentDataAsync(message.KnowledgeBaseId, message.DocumentId, cancellationToken);
}
