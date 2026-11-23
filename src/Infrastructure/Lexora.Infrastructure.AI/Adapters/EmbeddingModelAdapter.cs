using System.Net;
using Lexora.Application.Abstractions.Ai;
using Lexora.Infrastructure.AI.Registry;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.AI.Adapters;

public sealed class EmbeddingModelAdapter(EmbeddingGeneratorRegistry registry) : IEmbeddingModel
{
    public EmbeddingSpace Space { get; } = new(registry.Profile.Provider.ToString(), registry.Profile.Model, registry.Dimensions);

    public async Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> inputs, EmbeddingPurpose purpose, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inputs);

        var prefix = purpose == EmbeddingPurpose.Query ? registry.Profile.QueryPrefix : registry.Profile.DocumentPrefix;
        var options = registry.Provider.AcceptsDimensions ? new EmbeddingGenerationOptions { Dimensions = registry.Dimensions } : null;
        List<float[]> vectors = new(inputs.Count);

        foreach (var batch in inputs.Chunk(registry.Profile.MaxBatchSize))
        {
            var embeddings = await GenerateAsync([.. batch.Select(input => prefix + input)], options, cancellationToken);

            if (embeddings.Count != batch.Length)
            {
                throw new InvalidOperationException($"The embedding provider returned {embeddings.Count} vectors for {batch.Length} inputs.");
            }

            foreach (var embedding in embeddings)
            {
                vectors.Add(embedding.Vector.Length == registry.Dimensions
                    ? embedding.Vector.ToArray()
                    : throw new InvalidOperationException(
                        $"The embedding profile '{registry.ProfileName}' returned {embedding.Vector.Length} dimensions instead of {registry.Dimensions}."));
            }
        }

        return vectors;
    }

    private async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        string[] batch,
        EmbeddingGenerationOptions? options,
        CancellationToken cancellationToken)
    {
        try
        {
            return await registry.Generator.GenerateAsync(batch, options, cancellationToken);
        }
        catch (HttpRequestException exception) when (IsTransient(exception.StatusCode))
        {
            throw Unavailable(exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw Unavailable(exception);
        }
    }

    // A missing status code means the provider could not be reached at all, which is as transient as a 5xx.
    private static bool IsTransient(HttpStatusCode? status) =>
        status is null or HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout || (int)status >= 500;

    private AiProviderUnavailableException Unavailable(Exception exception) =>
        new(registry.Profile.Provider.ToString(), registry.ProfileName, retryAfter: null, exception);
}
