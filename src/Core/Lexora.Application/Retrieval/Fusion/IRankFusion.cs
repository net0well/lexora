using Lexora.Application.Abstractions.Search;

namespace Lexora.Application.Retrieval.Fusion;

public interface IRankFusion
{
    IReadOnlyList<FusedCandidate> Fuse(IReadOnlyList<ChunkSearchCandidate> vector, IReadOnlyList<ChunkSearchCandidate> keyword);
}
