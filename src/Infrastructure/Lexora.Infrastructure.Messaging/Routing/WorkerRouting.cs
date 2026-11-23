using Lexora.Application.Ingestion.EmbedDocumentChunks;
using Lexora.Domain.Documents.Events;
using Wolverine;
using Wolverine.RabbitMQ;

namespace Lexora.Infrastructure.Messaging.Routing;

internal static class WorkerRouting
{
    public static void Apply(WolverineOptions options, MessagingOptions messaging)
    {
        options.ListenToRabbitQueue(QueueNames.IngestionExtract)
            .MaximumParallelMessages(messaging.IngestionParallelism)
            .PreFetchCount(messaging.PreFetchCount);
        options.ListenToRabbitQueue(QueueNames.IngestionChunk)
            .MaximumParallelMessages(messaging.IngestionParallelism)
            .PreFetchCount(messaging.PreFetchCount);
        options.ListenToRabbitQueue(QueueNames.IngestionEmbed)
            .MaximumParallelMessages(messaging.EmbeddingParallelism)
            .PreFetchCount(messaging.PreFetchCount);
        options.ListenToRabbitQueue(QueueNames.Maintenance)
            .MaximumParallelMessages(messaging.IngestionParallelism)
            .PreFetchCount(messaging.PreFetchCount);

        options.PublishMessage<DocumentTextExtracted>().ToRabbitQueue(QueueNames.IngestionChunk);
        options.PublishMessage<DocumentChunked>().ToRabbitQueue(QueueNames.IngestionEmbed);
        options.PublishMessage<EmbedPendingChunks>().ToRabbitQueue(QueueNames.IngestionEmbed);
        options.PublishMessage<DocumentDeleted>().ToRabbitQueue(QueueNames.Maintenance);
    }
}
