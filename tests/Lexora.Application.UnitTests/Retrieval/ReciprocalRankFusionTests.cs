using Lexora.Application.Abstractions.Search;
using Lexora.Application.Common.Options;
using Lexora.Application.Retrieval.Fusion;
using Microsoft.Extensions.Options;

namespace Lexora.Application.UnitTests.Retrieval;

public sealed class ReciprocalRankFusionTests
{
    private readonly ReciprocalRankFusion _fusion = new(Options.Create(new RagOptions { RrfK = 60 }));

    [Fact]
    public void Fuse_ChunkInBothRankings_SumsItsReciprocalRanksAndComesFirst()
    {
        var shared = Candidate("shared");
        var onlyVector = Candidate("vector only", Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var onlyKeyword = Candidate("keyword only", Guid.Parse("00000000-0000-0000-0000-000000000002"));

        var fused = _fusion.Fuse([onlyVector, shared], [onlyKeyword, shared]);

        fused.Select(entry => entry.Candidate.Content).ShouldBe(["shared", "vector only", "keyword only"]);
        fused[0].Score.ShouldBe((1d / 62) + (1d / 62), tolerance: 1e-12);
        fused[0].MatchedBy.ShouldBe(SearchMatch.Both);
        fused[1].Score.ShouldBe(1d / 61, tolerance: 1e-12);
        (fused[1].MatchedBy, fused[2].MatchedBy).ShouldBe((SearchMatch.Vector, SearchMatch.Keyword));
    }

    [Fact]
    public void Fuse_TiedScores_PreferTheBetterPositionThenTheLowerId()
    {
        var secondInBoth = Candidate("second in both", Guid.Parse("00000000-0000-0000-0000-00000000000a"));
        var firstVectorThirdKeyword = Candidate("first and third", Guid.Parse("00000000-0000-0000-0000-00000000000b"));
        var filler = Candidate("filler", Guid.Parse("00000000-0000-0000-0000-00000000000c"));

        // 1/61 + 1/63 is slightly more than 2/62, so the chunk with the best single position wins.
        var fused = _fusion.Fuse([firstVectorThirdKeyword, secondInBoth], [filler, secondInBoth, firstVectorThirdKeyword]);

        fused.Select(entry => entry.Candidate.Content).Take(2).ShouldBe(["first and third", "second in both"]);
    }

    [Fact]
    public void Fuse_SameScoreAndPosition_FallsBackToTheChunkId()
    {
        var lower = Candidate("lower id", Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var higher = Candidate("higher id", Guid.Parse("00000000-0000-0000-0000-000000000002"));

        _fusion.Fuse([higher], [lower]).Select(entry => entry.Candidate.Content).ShouldBe(["lower id", "higher id"]);
        _fusion.Fuse([lower], [higher]).Select(entry => entry.Candidate.Content).ShouldBe(["lower id", "higher id"]);
    }

    [Fact]
    public void Fuse_DisjointRankings_InterleavesThemByPosition()
    {
        var fused = _fusion.Fuse(
            [Candidate("v1", Id(1)), Candidate("v2", Id(3))],
            [Candidate("k1", Id(2)), Candidate("k2", Id(4))]);

        fused.Select(entry => entry.Candidate.Content).ShouldBe(["v1", "k1", "v2", "k2"]);
        fused.Select(entry => entry.MatchedBy).Distinct().ShouldBe([SearchMatch.Vector, SearchMatch.Keyword], ignoreOrder: true);
    }

    [Fact]
    public void Fuse_EmptyRankings_ReturnNothing() => _fusion.Fuse([], []).ShouldBeEmpty();

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    public void Fuse_RrfK_SetsTheScaleOfTheScores(int k)
    {
        var fusion = new ReciprocalRankFusion(Options.Create(new RagOptions { RrfK = k }));

        fusion.Fuse([Candidate("only")], []).Single().Score.ShouldBe(1d / (k + 1), tolerance: 1e-12);
    }

    private static Guid Id(int value) => Guid.Parse($"00000000-0000-0000-0000-{value:D12}");

    internal static ChunkSearchCandidate Candidate(string content, Guid? id = null) =>
        new(id ?? Guid.CreateVersion7(), Guid.CreateVersion7(), "manual.pdf", content, 1, null, null, Score: 0.5);
}
