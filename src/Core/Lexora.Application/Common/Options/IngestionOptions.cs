using System.ComponentModel.DataAnnotations;

namespace Lexora.Application.Common.Options;

public sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";

    public TimeSpan StalledAfter { get; init; } = TimeSpan.FromMinutes(30);

    [Range(1, 1_000)]
    public int EmbeddingBatchSize { get; init; } = 32;
}
