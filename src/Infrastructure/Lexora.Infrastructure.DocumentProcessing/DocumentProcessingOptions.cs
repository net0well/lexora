using System.ComponentModel.DataAnnotations;

namespace Lexora.Infrastructure.DocumentProcessing;

public sealed class DocumentProcessingOptions
{
    public const string SectionName = "DocumentProcessing";

    [Range(1, 100_000)]
    public int MaxPages { get; init; } = 2_000;

    [Range(1, 50_000_000)]
    public int MaxCells { get; init; } = 500_000;
}
