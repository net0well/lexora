using Asp.Versioning.ApiExplorer;

namespace Lexora.Api.IntegrationTests.OpenApi;

internal sealed class StubApiVersionDescriptionProvider(params ApiVersionDescription[] descriptions) : IApiVersionDescriptionProvider
{
    public IReadOnlyList<ApiVersionDescription> ApiVersionDescriptions { get; } = descriptions;
}
