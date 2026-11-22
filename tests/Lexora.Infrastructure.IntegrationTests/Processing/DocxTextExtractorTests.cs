using System.Text;
using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Infrastructure.DocumentProcessing;
using Lexora.Infrastructure.DocumentProcessing.Extraction;
using Lexora.Shared.Results;
using Lexora.TestKit.Samples;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.IntegrationTests.Processing;

public sealed class DocxTextExtractorTests
{
    [Fact]
    public async Task ExtractAsync_Headings_StartNewSectionsAndTablesBecomeRows()
    {
        var docx = SampleDocx.Create(
            "Introduction before any heading.",
            "# Safety",
            "Wear gloves.",
            "| Item | Rule |",
            "| Gloves | Always |",
            "## Machines",
            "Turn them off.");

        var document = (await ExtractAsync(docx)).Value;

        document.Sections.Select(section => section.Title).ShouldBe([null, "Safety", "Machines"]);
        document.Sections[1].Text.ShouldBe("Wear gloves.\nItem | Rule\nGloves | Always");
        document.Sections.ShouldAllBe(section => section.PageNumber == null);
    }

    [Fact]
    public async Task ExtractAsync_LocalizedHeadingStyle_IsRecognizedByItsName()
    {
        var docx = SampleDocx.Create("@ Segurança", "Use luvas.");

        (await ExtractAsync(docx)).Value.Sections.ShouldHaveSingleItem().Title.ShouldBe("Segurança");
    }

    [Fact]
    public async Task ExtractAsync_PagesFromTheDocumentProperties_AreReportedAndLimited()
    {
        var docx = SampleDocx.Create(pages: 5, "text");

        (await ExtractAsync(docx)).Value.PageCount.ShouldBe(5);
        (await ExtractAsync(docx, maxPages: 4)).Notifications.ShouldBe([ExtractionFailures.PageLimit]);
    }

    [Fact]
    public async Task ExtractAsync_DamagedPackage_FailsAsUnreadable() =>
        (await ExtractAsync([0x50, 0x4B, 0x03, 0x04, .. Encoding.ASCII.GetBytes("broken zip body")])).Notifications
            .ShouldBe([ExtractionFailures.Unreadable]);

    private static async Task<Result<ExtractedDocument>> ExtractAsync(byte[] docx, int maxPages = 2_000)
    {
        using var stream = new MemoryStream(docx);

        return await new DocxTextExtractor(Options.Create(new DocumentProcessingOptions { MaxPages = maxPages }))
            .ExtractAsync(stream, DocumentFormat.Docx, TestContext.Current.CancellationToken);
    }
}
