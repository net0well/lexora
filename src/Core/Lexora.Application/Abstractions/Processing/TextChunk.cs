namespace Lexora.Application.Abstractions.Processing;

public sealed record TextChunk(
    int Index,
    string Content,
    int TokenCount,
    int? PageNumber,
    string? SheetName,
    string? SectionTitle,
    int StartOffset,
    int EndOffset);
