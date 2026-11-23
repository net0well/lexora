using Lexora.Infrastructure.AI.Options;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace Lexora.Infrastructure.AI.Providers.Ollama;

public sealed class OllamaEmbeddingGeneratorProvider : IEmbeddingGeneratorProvider
{
    public AiProviderKind Kind => AiProviderKind.Ollama;

    public bool AcceptsDimensions => false;

    public IEmbeddingGenerator<string, Embedding<float>> Create(string profileName, AiProfileSettings settings) =>
        new OllamaApiClient(
            ProviderHttpClient.Create(settings.Endpoint, TimeSpan.FromSeconds(settings.TimeoutSeconds ?? AiEmbeddingSettings.DefaultTimeoutSeconds)),
            settings.Model);
}
