using Lexora.Application.Abstractions.Ai;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Infrastructure.IntegrationTests.Ai.Contracts;

public sealed class EmbeddingProviderContractTests
{
    [Theory]
    [InlineData("local")]
    [InlineData("openai")]
    public async Task EmbedAsync_RealProvider_PlacesRelatedTextsCloserThanUnrelatedOnes(string profile)
    {
        await ProviderContractHost.SkipUnlessEmbeddingProfileWorksAsync(profile);
        using var host = new ProviderContractHost(("Ai:Embedding:Profile", profile));
        var embeddings = host.Services.GetRequiredService<IEmbeddingModel>();

        var vectors = await embeddings.EmbedAsync(
            ["vazamento de produto químico no laboratório", "derramamento de substância química", "política de férias dos funcionários"],
            EmbeddingPurpose.Document,
            TestContext.Current.CancellationToken);

        vectors.ShouldAllBe(vector => vector.Length == embeddings.Space.Dimensions);
        Cosine(vectors[0], vectors[1]).ShouldBeGreaterThan(Cosine(vectors[0], vectors[2]));
    }

    private static double Cosine(float[] left, float[] right)
    {
        double dot = 0, leftNorm = 0, rightNorm = 0;

        for (var index = 0; index < left.Length; index++)
        {
            dot += left[index] * right[index];
            leftNorm += left[index] * left[index];
            rightNorm += right[index] * right[index];
        }

        return dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm));
    }
}
