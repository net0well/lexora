using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers;
using Lexora.Infrastructure.AI.Telemetry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.AI.Registry;

public sealed class EmbeddingGeneratorRegistry : IDisposable
{
    private readonly Lazy<IEmbeddingGenerator<string, Embedding<float>>> _generator;

    public EmbeddingGeneratorRegistry(IEnumerable<IEmbeddingGeneratorProvider> providers, IOptions<AiOptions> options, ILoggerFactory loggerFactory)
    {
        var embedding = options.Value.Embedding;

        ProfileName = embedding.Profile;
        Profile = embedding.Active ?? throw new InvalidOperationException($"Unknown embedding profile '{embedding.Profile}'.");
        Dimensions = embedding.Dimensions;
        Provider = providers.SingleOrDefault(provider => provider.Kind == Profile.Provider)
            ?? throw new InvalidOperationException($"No embedding provider is registered for '{Profile.Provider}'.");

        _generator = new Lazy<IEmbeddingGenerator<string, Embedding<float>>>(
            () => AiInstrumentation.Instrument(Provider.Create(ProfileName, Profile), options.Value, loggerFactory));
    }

    public string ProfileName { get; }

    public AiProfileSettings Profile { get; }

    public IEmbeddingGeneratorProvider Provider { get; }

    public int Dimensions { get; }

    public IEmbeddingGenerator<string, Embedding<float>> Generator => _generator.Value;

    public void Dispose()
    {
        if (_generator.IsValueCreated)
        {
            _generator.Value.Dispose();
        }
    }
}
