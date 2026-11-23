using System.Net;
using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Ingestion.Services;
using Lexora.Worker.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lexora.Infrastructure.IntegrationTests.Worker;

public sealed class WorkerHostTests(WorkerFactory worker) : IClassFixture<WorkerFactory>
{
    [Fact]
    public async Task HealthLive_RunningWorker_RespondsOk()
    {
        using var client = worker.CreateClient();

        using var response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Services_PipelineDependencies_AreComposed()
    {
        await using var scope = worker.Services.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<IIngestionService>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IMaintenanceService>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IEmbeddingModel>().Space.Key.ShouldBe("fake:fake-embedding:1024");
        worker.Services.GetServices<IHostedService>().OfType<MaintenanceScheduler>().ShouldHaveSingleItem();
    }
}
