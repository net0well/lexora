using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Abstractions.Search;
using Lexora.Application.Common.Options;
using Lexora.Application.Retrieval;
using Lexora.Application.Retrieval.Fusion;
using Lexora.Application.Retrieval.Services;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Lexora.Application.UnitTests.Retrieval;

public sealed class RetrievalServiceTests
{
    private static readonly Guid _knowledgeBaseId = Guid.CreateVersion7();

    private readonly IChunkSearchIndex _index = Substitute.For<IChunkSearchIndex>();
    private readonly FakeEmbeddingModel _embeddings = new(dimensions: 4);
    private readonly InMemoryCacheStore _cache = new();
    private readonly RagOptions _options = new() { CandidatePoolSize = 30, MinVectorSimilarity = 0.3, MaxChunksPerDocument = 2 };

    public RetrievalServiceTests()
    {
        _index.VectorSearchAsync(default, default!, default!, default, default).ReturnsForAnyArgs([]);
        _index.KeywordSearchAsync(default, default!, default, default).ReturnsForAnyArgs([]);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RetrieveAsync_Hybrid_FusesBothRankingsWithTheQueryEmbedding()
    {
        var shared = Candidate("shared", score: 0.9);
        _index.VectorSearchAsync(_knowledgeBaseId, Arg.Any<float[]>(), "fake:fake-embedding:4", 30, Arg.Any<CancellationToken>())
            .Returns([Candidate("vector only", score: 0.95), shared]);
        _index.KeywordSearchAsync(_knowledgeBaseId, "vazamento químico", 30, Arg.Any<CancellationToken>()).Returns([shared, Candidate("keyword only")]);

        var hits = await RetrieveAsync(SearchMode.Hybrid);

        hits[0].ShouldSatisfyAllConditions(hit => hit.Content.ShouldBe("shared"), hit => hit.MatchedBy.ShouldBe(SearchMatch.Both));
        hits.Select(hit => hit.Content).ShouldBe(["shared", "vector only", "keyword only"], ignoreOrder: true);
        _embeddings.Calls.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            call => call.Inputs.ShouldBe(["vazamento químico"]),
            call => call.Purpose.ShouldBe(EmbeddingPurpose.Query));
    }

    [Fact]
    public async Task RetrieveAsync_VectorMode_DropsWeakMatchesAndSkipsKeywords()
    {
        _index.VectorSearchAsync(default, default!, default!, default, CancellationToken).ReturnsForAnyArgs([Candidate("close", score: 0.8), Candidate("weak", score: 0.29)]);

        var hits = await RetrieveAsync(SearchMode.Vector);

        hits.ShouldHaveSingleItem().Content.ShouldBe("close");
        await _index.DidNotReceiveWithAnyArgs().KeywordSearchAsync(default, default!, default, CancellationToken);
    }

    [Fact]
    public async Task RetrieveAsync_KeywordMode_DoesNotEmbedTheQuery()
    {
        _index.KeywordSearchAsync(default, default!, default, CancellationToken).ReturnsForAnyArgs([Candidate("NR-10")]);

        (await RetrieveAsync(SearchMode.Keyword)).ShouldHaveSingleItem().MatchedBy.ShouldBe(SearchMatch.Keyword);

        _embeddings.Calls.ShouldBeEmpty();
    }

    [Fact]
    public async Task RetrieveAsync_ManyChunksOfOneDocument_KeepsAFewPerDocumentUpToTopK()
    {
        var manual = Guid.CreateVersion7();
        _index.KeywordSearchAsync(default, default!, default, CancellationToken).ReturnsForAnyArgs(
        [
            Candidate("manual 1", document: manual),
            Candidate("manual 2", document: manual),
            Candidate("manual 3", document: manual),
            Candidate("policy 1"),
            Candidate("table 1"),
        ]);

        var hits = await RetrieveAsync(SearchMode.Keyword, topK: 3);

        hits.Select(hit => hit.Content).ShouldBe(["manual 1", "manual 2", "policy 1"]);
    }

    [Fact]
    public async Task RetrieveAsync_SameQueryTwice_EmbedsItOnceForTheActiveSpace()
    {
        await RetrieveAsync(SearchMode.Vector);
        await RetrieveAsync(SearchMode.Vector);

        _embeddings.Calls.ShouldHaveSingleItem();
        _cache.Keys.ShouldHaveSingleItem().ShouldBe(QueryEmbeddingCacheKey.For(_embeddings.Space, "vazamento químico"));
        _cache.Keys.Single().ShouldStartWith("query-embedding:fake:fake-embedding:4:");
    }

    private Task<IReadOnlyList<SearchHit>> RetrieveAsync(SearchMode mode, int topK = 8)
    {
        var reranker = Substitute.For<IReranker>();
        reranker.RerankAsync(default!, default!, default, default)
            .ReturnsForAnyArgs(call => Task.FromResult(call.ArgAt<IReadOnlyList<SearchHit>>(1)));
        var service = new RetrievalService(_index, _embeddings, _cache, new ReciprocalRankFusion(Options.Create(_options)), reranker, Options.Create(_options));

        return service.RetrieveAsync(_knowledgeBaseId, "vazamento químico", mode, topK, CancellationToken);
    }

    private static ChunkSearchCandidate Candidate(string content, double score = 0.5, Guid? document = null) =>
        new(Guid.CreateVersion7(), document ?? Guid.CreateVersion7(), "manual.pdf", content, 1, null, null, score);
}
