using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

internal sealed class StubEmbeddingGeneratorProvider(StubEmbeddingGenerator generator, bool acceptsDimensions = false) : IEmbeddingGeneratorProvider
{
    public AiProviderKind Kind => AiProviderKind.Ollama;

    public bool AcceptsDimensions => acceptsDimensions;

    public IEmbeddingGenerator<string, Embedding<float>> Create(string profileName, AiProfileSettings settings) => generator;
}
