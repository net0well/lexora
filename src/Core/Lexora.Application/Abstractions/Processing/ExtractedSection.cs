namespace Lexora.Application.Abstractions.Processing;

public sealed record ExtractedSection(string? Title, int? PageNumber, string? SheetName, string Text);
