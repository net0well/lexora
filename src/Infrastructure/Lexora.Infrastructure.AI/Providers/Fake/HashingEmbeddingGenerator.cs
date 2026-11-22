using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Providers.Fake;

public sealed partial class HashingEmbeddingGenerator(string model) : IEmbeddingGenerator<string, Embedding<float>>
{
    public const int DefaultDimensions = 1024;

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var dimensions = options?.Dimensions ?? DefaultDimensions;
        var embeddings = values.Select(value => new Embedding<float>(Vector(value, dimensions)) { ModelId = model });

        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    // Each word adds +1 or -1 to a hashed position, so texts sharing words point in similar directions.
    public static float[] Vector(string text, int dimensions)
    {
        var vector = new float[dimensions];

        foreach (Match word in Words().Matches(text.ToLowerInvariant()))
        {
            var hash = Fnv1a(word.Value);
            vector[(int)(hash % (uint)dimensions)] += (hash & 0x8000_0000) == 0 ? 1f : -1f;
        }

        var length = MathF.Sqrt(vector.Sum(value => value * value));

        if (length == 0)
        {
            vector[0] = 1f;
            return vector;
        }

        for (var index = 0; index < vector.Length; index++)
        {
            vector[index] /= length;
        }

        return vector;
    }

    private static uint Fnv1a(string word)
    {
        var hash = 2_166_136_261u;

        foreach (var character in word)
        {
            hash = unchecked((hash ^ character) * 16_777_619u);
        }

        return hash;
    }

    [GeneratedRegex(@"[\p{L}\p{Nd}]+")]
    private static partial Regex Words();
}
