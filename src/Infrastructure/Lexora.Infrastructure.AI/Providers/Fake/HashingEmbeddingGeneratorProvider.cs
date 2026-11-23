using Lexora.Infrastructure.AI.Options;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Providers.Fake;

public sealed class HashingEmbeddingGeneratorProvider : IEmbeddingGeneratorProvider
{
    public AiProviderKind Kind => AiProviderKind.Fake;

    public bool AcceptsDimensions => true;

    public IEmbeddingGenerator<string, Embedding<float>> Create(string profileName, AiProfileSettings settings) =>
        new HashingEmbeddingGenerator(settings.Model);
}
