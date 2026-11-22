using Lexora.Domain.Conversations;

namespace Lexora.Domain.UnitTests.Conversations;

public sealed class CitationTests
{
    private static readonly Guid _chunkId = Guid.CreateVersion7();
    private static readonly Guid _documentId = Guid.CreateVersion7();

    [Fact]
    public void Create_ValidInput_KeepsValues()
    {
        var citation = Citation.Create(2, _chunkId, _documentId, "prices.xlsx", null, "2026", "Unit price: 12.50", 0.016);

        citation.Index.ShouldBe(2);
        citation.ChunkId.ShouldBe(_chunkId);
        citation.DocumentId.ShouldBe(_documentId);
        citation.DocumentName.ShouldBe("prices.xlsx");
        citation.PageNumber.ShouldBeNull();
        citation.SheetName.ShouldBe("2026");
        citation.Excerpt.ShouldBe("Unit price: 12.50");
        citation.Score.ShouldBe(0.016);
    }

    [Fact]
    public void Create_BlankSheetName_StoresNull() =>
        Citation.Create(1, _chunkId, _documentId, "contract.pdf", 3, " ", "Excerpt", 0.1).SheetName.ShouldBeNull();

    [Fact]
    public void Create_ExcerptAtLimit_Succeeds() =>
        Citation.Create(1, _chunkId, _documentId, "contract.pdf", 1, null, new string('e', Citation.MaxExcerptLength), 0.1)
            .Excerpt.Length.ShouldBe(Citation.MaxExcerptLength);

    [Fact]
    public void Create_ExcerptOverLimit_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Citation.Create(1, _chunkId, _documentId, "contract.pdf", 1, null, new string('e', Citation.MaxExcerptLength + 1), 0.1));

    [Theory]
    [InlineData(0, 1, 0.1)]
    [InlineData(1, 0, 0.1)]
    [InlineData(1, 1, -0.1)]
    [InlineData(1, 1, double.NaN)]
    [InlineData(1, 1, double.PositiveInfinity)]
    public void Create_InvalidNumber_Throws(int index, int pageNumber, double score) =>
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Citation.Create(index, _chunkId, _documentId, "contract.pdf", pageNumber, null, "Excerpt", score));

    [Fact]
    public void Equals_SameValues_ReturnsTrue() =>
        Citation.Create(1, _chunkId, _documentId, "contract.pdf", 1, null, "Excerpt", 0.1)
            .ShouldBe(Citation.Create(1, _chunkId, _documentId, "contract.pdf", 1, null, "Excerpt", 0.1));
}
