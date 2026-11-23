using Lexora.Application.Ingestion.EmbedDocumentChunks;
using Lexora.Application.Ingestion.Maintenance;
using Lexora.Domain.Documents.Events;
using Lexora.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.Runtime;

namespace Lexora.Infrastructure.IntegrationTests.Worker;

public sealed class WorkerRoutingTests(WorkerFactory worker) : IClassFixture<WorkerFactory>
{
    private static readonly Guid _knowledgeBaseId = Guid.CreateVersion7();
    private static readonly Guid _documentId = Guid.CreateVersion7();

    public static TheoryData<object, string> Routes => new()
    {
        { new DocumentTextExtracted(_knowledgeBaseId, _documentId, 1, TestClock.Now), "rabbitmq://queue/lexora.ingestion.chunk" },
        { new DocumentChunked(_knowledgeBaseId, _documentId, 1, TestClock.Now), "rabbitmq://queue/lexora.ingestion.embed" },
        { new EmbedPendingChunks(_documentId, 1), "rabbitmq://queue/lexora.ingestion.embed" },
        { new DocumentDeleted(_knowledgeBaseId, _documentId, 1, TestClock.Now), "rabbitmq://queue/lexora.maintenance" },
        { new DocumentIndexed(_knowledgeBaseId, _documentId, 1, TestClock.Now), "local://" },
        { new DocumentFailed(_knowledgeBaseId, _documentId, 1, TestClock.Now), "local://" },
        { new SweepStalledDocuments(), "local://" },
        { new SweepOrphanFiles(), "local://" },
        { new SweepExpiredRefreshTokens(), "local://" },
    };

    [Theory]
    [MemberData(nameof(Routes))]
    public void Publish_PipelineMessage_GoesToItsDestination(object message, string expectedDestination)
    {
        var runtime = worker.Services.GetRequiredService<IWolverineRuntime>();

        var destination = runtime.RoutingFor(message.GetType()).RouteForPublish(message, options: null).ShouldHaveSingleItem().Destination!;

        destination.ToString().ShouldStartWith(expectedDestination);
    }

    [Fact]
    public void LocalPipelineMessages_UseDurableQueues()
    {
        var runtime = worker.Services.GetRequiredService<IWolverineRuntime>();

        var endpoint = runtime.Endpoints.EndpointFor(runtime.RoutingFor(typeof(DocumentIndexed))
            .RouteForPublish(new DocumentIndexed(_knowledgeBaseId, _documentId, 1, TestClock.Now), options: null).Single().Destination!);

        endpoint.ShouldNotBeNull().Mode.ShouldBe(Wolverine.Configuration.EndpointMode.Durable);
    }
}
