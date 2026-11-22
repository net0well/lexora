using System.Net;
using Lexora.Application.Abstractions.Ai;
using Lexora.Infrastructure.AI.Adapters;
using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Registry;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class EmbeddingModelAdapterTests
{
    private const int Dimensions = 8;

    [Fact]
    public void Space_ActiveProfile_IsDescribedByProviderModelAndDimensions() =>
        Adapter(new StubEmbeddingGenerator(Dimensions)).Space.Key.ShouldBe("ollama:bge-m3:8");

    [Fact]
    public async Task EmbedAsync_MoreInputsThanTheBatchSize_SendsSeveralBatchesInOrder()
    {
        var generator = new StubEmbeddingGenerator(Dimensions);

        var vectors = await Adapter(generator, maxBatchSize: 2).EmbedAsync(["a", "bb", "ccc"], EmbeddingPurpose.Document, TestContext.Current.CancellationToken);

        generator.Batches.Select(batch => batch.Length).ShouldBe([2, 1]);
        vectors.Select(vector => vector[0]).ShouldBe([1f, 2f, 3f]);
        vectors.ShouldAllBe(vector => vector.Length == Dimensions);
    }

    [Theory]
    [InlineData(EmbeddingPurpose.Query, "search_query: question")]
    [InlineData(EmbeddingPurpose.Document, "search_document: question")]
    public async Task EmbedAsync_Purpose_AppliesTheProfilePrefix(EmbeddingPurpose purpose, string expected)
    {
        var generator = new StubEmbeddingGenerator(Dimensions);

        await Adapter(generator, queryPrefix: "search_query: ", documentPrefix: "search_document: ")
            .EmbedAsync(["question"], purpose, TestContext.Current.CancellationToken);

        generator.Batches.Single().ShouldBe([expected]);
    }

    [Fact]
    public async Task EmbedAsync_ProviderAcceptingDimensions_IsAskedForThem()
    {
        var generator = new StubEmbeddingGenerator(Dimensions);

        await Adapter(generator, acceptsDimensions: true).EmbedAsync(["a"], EmbeddingPurpose.Document, TestContext.Current.CancellationToken);

        generator.LastOptions.ShouldNotBeNull().Dimensions.ShouldBe(Dimensions);
    }

    [Fact]
    public async Task EmbedAsync_VectorOfTheWrongSize_IsRejected()
    {
        var adapter = Adapter(new StubEmbeddingGenerator(Dimensions + 1));

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            adapter.EmbedAsync(["a"], EmbeddingPurpose.Document, TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("9 dimensions instead of 8");
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(null)]
    public async Task EmbedAsync_TransientProviderFailure_SurfacesAsUnavailable(HttpStatusCode? status)
    {
        var generator = new StubEmbeddingGenerator(Dimensions) { FailWith = new HttpRequestException("failed", null, status) };

        var exception = await Should.ThrowAsync<AiProviderUnavailableException>(() =>
            Adapter(generator).EmbedAsync(["a"], EmbeddingPurpose.Document, TestContext.Current.CancellationToken));

        (exception.Provider, exception.Profile).ShouldBe(("Ollama", "local"));
    }

    [Fact]
    public async Task EmbedAsync_Timeout_SurfacesAsUnavailable()
    {
        var generator = new StubEmbeddingGenerator(Dimensions) { FailWith = new TaskCanceledException("timed out") };

        await Should.ThrowAsync<AiProviderUnavailableException>(() =>
            Adapter(generator).EmbedAsync(["a"], EmbeddingPurpose.Document, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EmbedAsync_ConfigurationError_IsNotTreatedAsTransient()
    {
        var generator = new StubEmbeddingGenerator(Dimensions) { FailWith = new HttpRequestException("model not found", null, HttpStatusCode.NotFound) };

        await Should.ThrowAsync<HttpRequestException>(() =>
            Adapter(generator).EmbedAsync(["a"], EmbeddingPurpose.Document, TestContext.Current.CancellationToken));
    }

    private static EmbeddingModelAdapter Adapter(
        StubEmbeddingGenerator generator,
        int maxBatchSize = 32,
        string queryPrefix = "",
        string documentPrefix = "",
        bool acceptsDimensions = false)
    {
        var options = new AiOptions
        {
            Embedding = new AiEmbeddingSettings
            {
                Profile = "local",
                Dimensions = Dimensions,
                Profiles =
                {
                    ["local"] = new AiProfileSettings
                    {
                        Provider = AiProviderKind.Ollama,
                        Model = "bge-m3",
                        Endpoint = new Uri("http://localhost:11434"),
                        MaxBatchSize = maxBatchSize,
                        QueryPrefix = queryPrefix,
                        DocumentPrefix = documentPrefix,
                    },
                },
            },
        };

        return new EmbeddingModelAdapter(new EmbeddingGeneratorRegistry([new StubEmbeddingGeneratorProvider(generator, acceptsDimensions)], Options.Create(options)));
    }
}
