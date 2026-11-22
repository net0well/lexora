using Lexora.Infrastructure.AI.Options;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace Lexora.Infrastructure.AI.Providers.Ollama;

public sealed class OllamaEmbeddingGeneratorProvider : IEmbeddingGeneratorProvider
{
    public AiProviderKind Kind => AiProviderKind.Ollama;

    public bool AcceptsDimensions => false;

    public IEmbeddingGenerator<string, Embedding<float>> Create(string profileName, AiProfileSettings settings)
    {
        var http = new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) })
        {
            BaseAddress = settings.Endpoint,
            Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds ?? AiEmbeddingSettings.DefaultTimeoutSeconds),
        };

        return new OllamaApiClient(http, settings.Model);
    }
}
