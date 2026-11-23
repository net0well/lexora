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

    internal static ChunkSearchCandidate Candidate(string content, Guid? id = null) =>
        new(id ?? Guid.CreateVersion7(), Guid.CreateVersion7(), "manual.pdf", content, 1, null, null, Score: 0.5);
}
