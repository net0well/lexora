extern alias Worker;

using Lexora.Application.Abstractions.Ai;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lexora.Api.IntegrationTests.Pipeline;

public sealed class PipelineWorkerFactory(PipelineSettings settings, IEmbeddingModel embeddings) : WebApplicationFactory<Worker::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => PipelineHostConfiguration.Apply(builder, settings, embeddings);
}
