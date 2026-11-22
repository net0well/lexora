using Lexora.Shared.Domain;
using Lexora.Shared.Guards;

namespace Lexora.Domain.Chunks;

public sealed class ChunkLocation : ValueObject
{
    private ChunkLocation(int? pageNumber, string? sheetName, string? sectionTitle, int startOffset, int endOffset)
    {
        PageNumber = pageNumber;
        SheetName = sheetName;
        SectionTitle = sectionTitle;
        StartOffset = startOffset;
        EndOffset = endOffset;
    }

    public int? PageNumber { get; }

    public string? SheetName { get; }

    public string? SectionTitle { get; }

    public int StartOffset { get; }

    public int EndOffset { get; }

    public static ChunkLocation Create(int? pageNumber, string? sheetName, string? sectionTitle, int startOffset, int endOffset)
    {
        if (pageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), pageNumber, "Page numbers start at 1.");
        }

        Guard.NotNegative(startOffset);

        if (endOffset < startOffset)
        {
            throw new ArgumentOutOfRangeException(nameof(endOffset), endOffset, "The end offset cannot precede the start offset.");
        }

        return new ChunkLocation(pageNumber, NullIfBlank(sheetName), NullIfBlank(sectionTitle), startOffset, endOffset);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PageNumber;
        yield return SheetName;
        yield return SectionTitle;
        yield return StartOffset;
        yield return EndOffset;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
