using Lexora.Application.Abstractions.Search;

namespace Lexora.Application.Retrieval.Fusion;

public sealed record FusedCandidate(ChunkSearchCandidate Candidate, double Score, SearchMatch MatchedBy);
