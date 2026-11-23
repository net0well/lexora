using Lexora.Infrastructure.AI.Options;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Providers.OpenAi;

public sealed class OpenAiEmbeddingGeneratorProvider : IEmbeddingGeneratorProvider
{
    public AiProviderKind Kind => AiProviderKind.OpenAI;

    public bool AcceptsDimensions => true;

    public IEmbeddingGenerator<string, Embedding<float>> Create(string profileName, AiProfileSettings settings) =>
        OpenAiClientFactory.Create(settings, TimeSpan.FromSeconds(settings.TimeoutSeconds ?? AiEmbeddingSettings.DefaultTimeoutSeconds))
            .GetEmbeddingClient(settings.Model)
            .AsIEmbeddingGenerator();
}
