namespace Lexora.Infrastructure.Persistence.Search;

public sealed class ChunkSearchRow
{
    public Guid ChunkId { get; init; }

    public Guid DocumentId { get; init; }

    public string DocumentName { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public int? PageNumber { get; init; }

    public string? SheetName { get; init; }

    public string? SectionTitle { get; init; }

    public double Score { get; init; }
}
