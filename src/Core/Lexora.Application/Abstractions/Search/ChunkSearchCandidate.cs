namespace Lexora.Application.Abstractions.Search;

public sealed record ChunkSearchCandidate(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentName,
    string Content,
    int? PageNumber,
    string? SheetName,
    string? SectionTitle,
    double Score);
