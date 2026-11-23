using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers.Ollama;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class OllamaProvidersTests
{
    private static readonly AiProfileSettings _profile = new() { Provider = AiProviderKind.Ollama, Model = "qwen2.5:7b", Endpoint = new Uri("http://ollama:11434") };

    [Fact]
    public void Create_ChatProfile_PointsTheClientAtTheProfileEndpointAndModel()
    {
        using var client = new OllamaChatClientProvider().Create("local", _profile);

        var metadata = client.GetService<ChatClientMetadata>().ShouldNotBeNull();
        metadata.ProviderName.ShouldBe("ollama");
        metadata.ProviderUri.ShouldBe(new Uri("http://ollama:11434"));
        metadata.DefaultModelId.ShouldBe("qwen2.5:7b");
    }

    [Fact]
    public void Create_EmbeddingProfile_PointsTheGeneratorAtTheProfileEndpointAndModel()
    {
        using var generator = new OllamaEmbeddingGeneratorProvider().Create("local", _profile);

        var metadata = generator.GetService<EmbeddingGeneratorMetadata>().ShouldNotBeNull();
        metadata.ProviderUri.ShouldBe(new Uri("http://ollama:11434"));
        metadata.DefaultModelId.ShouldBe("qwen2.5:7b");
    }
}
