using Lexora.Shared.Domain;
using Lexora.Shared.Guards;

namespace Lexora.Domain.Conversations;

public sealed class Citation : ValueObject
{
    public const int MaxExcerptLength = 300;

    private Citation(
        int index,
        Guid chunkId,
        Guid documentId,
        string documentName,
        int? pageNumber,
        string? sheetName,
        string excerpt,
        double score)
    {
        Index = index;
        ChunkId = chunkId;
        DocumentId = documentId;
        DocumentName = documentName;
        PageNumber = pageNumber;
        SheetName = sheetName;
        Excerpt = excerpt;
        Score = score;
    }

    public int Index { get; }

    public Guid ChunkId { get; }

    public Guid DocumentId { get; }

    public string DocumentName { get; }

    public int? PageNumber { get; }

    public string? SheetName { get; }

    public string Excerpt { get; }

    public double Score { get; }

    public static Citation Create(
        int index,
        Guid chunkId,
        Guid documentId,
        string documentName,
        int? pageNumber,
        string? sheetName,
        string excerpt,
        double score)
    {
        if (pageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), pageNumber, "Page numbers start at 1.");
        }

        if (!double.IsFinite(score) || score < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(score), score, "The score must be a finite, non-negative number.");
        }

        return new Citation(
            Guard.Positive(index),
            Guard.NotEmpty(chunkId),
            Guard.NotEmpty(documentId),
            Guard.NotNullOrWhiteSpace(documentName),
            pageNumber,
            string.IsNullOrWhiteSpace(sheetName) ? null : sheetName,
            Guard.MaxLength(Guard.NotNullOrWhiteSpace(excerpt), MaxExcerptLength),
            score);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Index;
        yield return ChunkId;
        yield return DocumentId;
        yield return DocumentName;
        yield return PageNumber;
        yield return SheetName;
        yield return Excerpt;
        yield return Score;
    }
}
