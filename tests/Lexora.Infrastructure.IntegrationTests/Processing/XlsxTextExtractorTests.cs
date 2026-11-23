using System.Text;
using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Infrastructure.DocumentProcessing;
using Lexora.Infrastructure.DocumentProcessing.Extraction;
using Lexora.Shared.Results;
using Lexora.TestKit.Samples;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.IntegrationTests.Processing;

public sealed class XlsxTextExtractorTests
{
    [Fact]
    public async Task ExtractAsync_Sheets_BecomeSectionsWithHeaderValueRows()
    {
        var xlsx = SampleXlsx.Create(
            ("Prices", [["Item", "Price"], ["Bolt", "1.5"], ["Nut", ""]]),
            ("Empty", []),
            ("Totals", [["Name", "Total"], ["All", "=2+3"]]));

        var document = (await ExtractAsync(xlsx)).Value;

        document.PageCount.ShouldBe(3);
        document.Sections.Select(section => section.SheetName).ShouldBe(["Prices", "Totals"]);
        document.Sections[0].Text.ShouldBe("Item: Bolt; Price: 1.5\nItem: Nut");
        document.Sections[1].Text.ShouldBe("Name: All; Total: 5");
    }

    [Fact]
    public async Task ExtractAsync_DataStartingBelowEmptyRows_UsesTheFirstUsedRowAsHeader()
    {
        var xlsx = SampleXlsx.Create(("Report", [[], [], ["Code", "Status"], ["A1", "Open"]]));

        (await ExtractAsync(xlsx)).Value.Sections.Single().Text.ShouldBe("Code: A1; Status: Open");
    }

    [Fact]
    public async Task ExtractAsync_MoreCellsThanTheLimit_FailsWithTheCellLimit()
    {
        var xlsx = SampleXlsx.Create(("Big", [["a", "b"], ["1", "2"], ["3", "4"]]));

        (await ExtractAsync(xlsx, maxCells: 5)).Notifications.ShouldBe([ExtractionFailures.CellLimit]);
    }

    [Fact]
    public async Task ExtractAsync_DamagedWorkbook_FailsAsUnreadable() =>
        (await ExtractAsync([0x50, 0x4B, 0x03, 0x04, .. Encoding.ASCII.GetBytes("not a workbook")])).Notifications
            .ShouldBe([ExtractionFailures.Unreadable]);

    private static async Task<Result<ExtractedDocument>> ExtractAsync(byte[] xlsx, int maxCells = 500_000)
    {
        using var stream = new MemoryStream(xlsx);

        return await new XlsxTextExtractor(Options.Create(new DocumentProcessingOptions { MaxCells = maxCells }))
            .ExtractAsync(stream, DocumentFormat.Xlsx, TestContext.Current.CancellationToken);
    }
}
