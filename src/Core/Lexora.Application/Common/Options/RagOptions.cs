using System.ComponentModel.DataAnnotations;

namespace Lexora.Application.Common.Options;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    [Range(1, 200)]
    public int CandidatePoolSize { get; init; } = 30;

    [Range(0d, 1d)]
    public double MinVectorSimilarity { get; init; } = 0.30;

    [Range(1, 1_000)]
    public int RrfK { get; init; } = 60;

    [Range(1, 50)]
    public int MaxChunksPerDocument { get; init; } = 3;

    [Range(1, 50)]
    public int TopK { get; init; } = 8;

    [Range(500, 200_000)]
    public int MaxContextTokens { get; init; } = 6_000;

    [Range(0, 50)]
    public int HistoryMessages { get; init; } = 6;

    public bool RewriteFollowUpQuestions { get; init; } = true;
}
