using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers.OpenAi;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Providers.OpenAiCompatible;

public sealed class OpenAiCompatibleEmbeddingGeneratorProvider : IEmbeddingGeneratorProvider
{
    public AiProviderKind Kind => AiProviderKind.OpenAICompatible;

    // Compatible services rarely support shortening vectors, so the model must already produce the configured size.
    public bool AcceptsDimensions => false;

    public IEmbeddingGenerator<string, Embedding<float>> Create(string profileName, AiProfileSettings settings) =>
        OpenAiClientFactory.Create(settings, TimeSpan.FromSeconds(settings.TimeoutSeconds ?? AiEmbeddingSettings.DefaultTimeoutSeconds))
            .GetEmbeddingClient(settings.Model)
            .AsIEmbeddingGenerator();
}
