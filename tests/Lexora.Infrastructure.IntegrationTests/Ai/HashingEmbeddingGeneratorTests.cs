using Lexora.Application.Abstractions.Ai;
using Lexora.Infrastructure.AI.Adapters;
using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers.Fake;
using Lexora.Infrastructure.AI.Registry;
using Lexora.Infrastructure.AI.Resilience;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class HashingEmbeddingGeneratorTests
{
    [Fact]
    public void Vector_SameText_IsDeterministicAndNormalized()
    {
        var first = HashingEmbeddingGenerator.Vector("Wear gloves in the lab", 64);
        var second = HashingEmbeddingGenerator.Vector("wear GLOVES in the lab", 64);

        first.ShouldBe(second);
        MathF.Sqrt(first.Sum(value => value * value)).ShouldBe(1f, tolerance: 0.0001f);
    }

    [Fact]
    public void Vector_TextsSharingWords_AreCloserThanUnrelatedOnes()
    {
        var question = HashingEmbeddingGenerator.Vector("which gloves are required in the laboratory", 256);
        var related = HashingEmbeddingGenerator.Vector("nitrile gloves are required in the laboratory", 256);
        var unrelated = HashingEmbeddingGenerator.Vector("quarterly budget for marketing campaigns", 256);

        Cosine(question, related).ShouldBeGreaterThan(Cosine(question, unrelated));
    }

    [Fact]
    public void Vector_TextWithoutWords_IsStillAUnitVector() =>
        HashingEmbeddingGenerator.Vector("  ...  ", 8).ShouldBe([1f, 0, 0, 0, 0, 0, 0, 0]);

    [Fact]
    public async Task FakeProfile_ThroughTheAdapter_ProducesTheConfiguredDimensions()
    {
        var options = new AiOptions
        {
            Embedding = new AiEmbeddingSettings
            {
                Profile = "fake",
                Dimensions = 1024,
                Profiles = { ["fake"] = new AiProfileSettings { Provider = AiProviderKind.Fake, Model = "fake-embedding" } },
            },
        };
        var adapter = new EmbeddingModelAdapter(new EmbeddingGeneratorRegistry([new HashingEmbeddingGeneratorProvider()], Options.Create(options)), new AiResiliencePipelineProvider(TimeProvider.System));

        var vectors = await adapter.EmbedAsync(["one", "two"], EmbeddingPurpose.Document, TestContext.Current.CancellationToken);

        adapter.Space.Key.ShouldBe("fake:fake-embedding:1024");
        vectors.ShouldAllBe(vector => vector.Length == 1024);
    }

    private static float Cosine(float[] left, float[] right) => left.Zip(right, (a, b) => a * b).Sum();
}
