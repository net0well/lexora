using Lexora.Application.Retrieval.Fusion;

namespace Lexora.Application.Retrieval;

public sealed record SearchHit(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentName,
    string Content,
    int? PageNumber,
    string? SheetName,
    string? SectionTitle,
    double Score,
    SearchMatch MatchedBy);
