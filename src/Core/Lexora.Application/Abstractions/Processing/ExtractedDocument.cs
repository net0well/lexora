namespace Lexora.Application.Abstractions.Processing;

public sealed record ExtractedDocument(IReadOnlyList<ExtractedSection> Sections, int PageCount);
