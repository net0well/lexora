using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Domain.Documents.Events;
using Lexora.Domain.KnowledgeBases.Events;
using Lexora.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.Runtime;

namespace Lexora.Api.IntegrationTests.Messaging;

public sealed class ApiRoutingTests(ApiFactory api)
{
    private static readonly Guid _knowledgeBaseId = Guid.CreateVersion7();
    private static readonly Guid _documentId = Guid.CreateVersion7();

    public static TheoryData<object, string> Events => new()
    {
        { new DocumentUploaded(_knowledgeBaseId, _documentId, 1, TestClock.Now), "rabbitmq://queue/lexora.ingestion.extract" },
        { new DocumentReprocessRequested(_knowledgeBaseId, _documentId, 2, TestClock.Now), "rabbitmq://queue/lexora.ingestion.extract" },
        { new DocumentDeleted(_knowledgeBaseId, _documentId, 1, TestClock.Now), "rabbitmq://queue/lexora.maintenance" },
        { new KnowledgeBaseArchived(_knowledgeBaseId, TestClock.Now), "rabbitmq://queue/lexora.maintenance" },
    };

    [Theory]
    [MemberData(nameof(Events))]
    public void Publish_DomainEvent_GoesOnlyToItsRabbitQueue(object message, string expectedDestination)
    {
        var runtime = api.Services.GetRequiredService<IWolverineRuntime>();

        var envelopes = runtime.RoutingFor(message.GetType()).RouteForPublish(message, options: null);

        envelopes.ShouldHaveSingleItem().Destination.ShouldBe(new Uri(expectedDestination));
    }
}
