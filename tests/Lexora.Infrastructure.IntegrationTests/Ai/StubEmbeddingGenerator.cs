using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

internal sealed class StubEmbeddingGenerator(int dimensions) : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly List<string[]> _batches = [];

    public IReadOnlyList<string[]> Batches => _batches;

    public Exception? FailWith { get; set; }

    public EmbeddingGenerationOptions? LastOptions { get; private set; }

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        var batch = values.ToArray();
        _batches.Add(batch);
        LastOptions = options;

        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(
            batch.Select(value => new Embedding<float>(Enumerable.Repeat((float)value.Length, dimensions).ToArray()))));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
