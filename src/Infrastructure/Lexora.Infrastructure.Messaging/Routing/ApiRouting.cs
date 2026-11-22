using Lexora.Domain.Documents.Events;
using Lexora.Domain.KnowledgeBases.Events;
using Wolverine;
using Wolverine.RabbitMQ;

namespace Lexora.Infrastructure.Messaging.Routing;

internal static class ApiRouting
{
    public static void Apply(WolverineOptions options)
    {
        options.PublishMessage<DocumentUploaded>().ToRabbitQueue(QueueNames.IngestionExtract);
        options.PublishMessage<DocumentReprocessRequested>().ToRabbitQueue(QueueNames.IngestionExtract);
        options.PublishMessage<DocumentDeleted>().ToRabbitQueue(QueueNames.Maintenance);
        options.PublishMessage<KnowledgeBaseArchived>().ToRabbitQueue(QueueNames.Maintenance);
    }
}
