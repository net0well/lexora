using Lexora.Application.Abstractions.Search;
using Lexora.Application.Common.Options;
using Microsoft.Extensions.Options;

namespace Lexora.Application.Retrieval.Fusion;

public sealed class ReciprocalRankFusion(IOptions<RagOptions> options) : IRankFusion
{
    public IReadOnlyList<FusedCandidate> Fuse(IReadOnlyList<ChunkSearchCandidate> vector, IReadOnlyList<ChunkSearchCandidate> keyword)
    {
        ArgumentNullException.ThrowIfNull(vector);
        ArgumentNullException.ThrowIfNull(keyword);

        var k = options.Value.RrfK;
        Dictionary<Guid, (ChunkSearchCandidate Candidate, double Score, SearchMatch MatchedBy, int BestRank)> fused = [];

        Add(vector, SearchMatch.Vector);
        Add(keyword, SearchMatch.Keyword);

        // Ties are broken by the best position in any list and then by id, so the same input always ranks the same way.
        return
        [
            .. fused.Values
                .OrderByDescending(entry => entry.Score)
                .ThenBy(entry => entry.BestRank)
                .ThenBy(entry => entry.Candidate.ChunkId)
                .Select(entry => new FusedCandidate(entry.Candidate, entry.Score, entry.MatchedBy)),
        ];

        void Add(IReadOnlyList<ChunkSearchCandidate> ranking, SearchMatch source)
        {
            for (var position = 0; position < ranking.Count; position++)
            {
                var candidate = ranking[position];
                var rank = position + 1;
                var contribution = 1d / (k + rank);

                fused[candidate.ChunkId] = fused.TryGetValue(candidate.ChunkId, out var current)
                    ? (current.Candidate, current.Score + contribution, current.MatchedBy | source, Math.Min(current.BestRank, rank))
                    : (candidate, contribution, source, rank);
            }
        }
    }
}
