using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lexora.Api.IntegrationTests.Pipeline;

public sealed class PipelineApiFactory(PipelineSettings settings) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => PipelineHostConfiguration.Apply(builder, settings);
}
