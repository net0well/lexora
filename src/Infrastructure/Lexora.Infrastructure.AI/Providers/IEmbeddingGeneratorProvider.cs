using Lexora.Infrastructure.AI.Options;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Providers;

public interface IEmbeddingGeneratorProvider
{
    AiProviderKind Kind { get; }

    bool AcceptsDimensions { get; }

    IEmbeddingGenerator<string, Embedding<float>> Create(string profileName, AiProfileSettings settings);
}
