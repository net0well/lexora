using Lexora.Domain.Chunks;

namespace Lexora.Domain.UnitTests.Chunks;

public sealed class ChunkLocationTests
{
    [Fact]
    public void Create_ValidPosition_KeepsValues()
    {
        var location = ChunkLocation.Create(3, "Revenue", "Summary", 10, 250);

        location.PageNumber.ShouldBe(3);
        location.SheetName.ShouldBe("Revenue");
        location.SectionTitle.ShouldBe("Summary");
        location.StartOffset.ShouldBe(10);
        location.EndOffset.ShouldBe(250);
    }

    [Fact]
    public void Create_BlankNames_StoresNull()
    {
        var location = ChunkLocation.Create(null, "  ", "", 0, 0);

        location.SheetName.ShouldBeNull();
        location.SectionTitle.ShouldBeNull();
    }

    [Fact]
    public void Create_NamesWithSpaces_Trims() => ChunkLocation.Create(null, " Revenue ", null, 0, 1).SheetName.ShouldBe("Revenue");

    [Theory]
    [InlineData(0, 0, 10)]
    [InlineData(1, -1, 10)]
    [InlineData(1, 20, 10)]
    public void Create_InvalidPosition_Throws(int pageNumber, int startOffset, int endOffset) =>
        Should.Throw<ArgumentOutOfRangeException>(() => ChunkLocation.Create(pageNumber, null, null, startOffset, endOffset));

    [Fact]
    public void Equals_SamePosition_ReturnsTrue() =>
        ChunkLocation.Create(1, null, "Intro", 0, 99).ShouldBe(ChunkLocation.Create(1, null, "Intro", 0, 99));
}
