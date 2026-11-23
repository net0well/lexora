using Lexora.Application.Ingestion.Services;
using Lexora.Domain.KnowledgeBases.Events;
using Microsoft.Extensions.Logging;

namespace Lexora.Application.Ingestion.Maintenance;

public sealed class KnowledgeBaseArchivedHandler(ILogger<KnowledgeBaseArchivedHandler> logger)
{
    public void Handle(KnowledgeBaseArchived message) => logger.KnowledgeBaseArchived(message.KnowledgeBaseId);
}
