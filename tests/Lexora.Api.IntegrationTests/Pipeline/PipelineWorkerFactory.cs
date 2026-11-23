extern alias Worker;

using Lexora.Application.Abstractions.Ai;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lexora.Api.IntegrationTests.Pipeline;

public sealed class PipelineWorkerFactory(PipelineSettings settings, IEmbeddingModel embeddings) : WebApplicationFactory<Worker::Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        PipelineHostConfiguration.Apply(builder, settings);
        builder.ConfigureTestServices(services => services.Replace(ServiceDescriptor.Singleton<IEmbeddingModel>(embeddings)));
    }
}
