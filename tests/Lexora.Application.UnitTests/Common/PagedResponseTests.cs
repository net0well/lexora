using Lexora.Application.Common.Paging;

namespace Lexora.Application.UnitTests.Common;

public sealed class PagedResponseTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(5, 2, 3)]
    public void Create_TotalItems_RoundsTheTotalPagesUp(int totalItems, int pageSize, int expectedPages) =>
        PagedResponse.Create<int>([], new PageRequest(1, pageSize), totalItems).TotalPages.ShouldBe(expectedPages);

    [Fact]
    public void Skip_SecondPage_SkipsTheFirstPage() => new PageRequest(2, 20).Skip.ShouldBe(20);
}
