using System.Text;
using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Infrastructure.DocumentProcessing;
using Lexora.Infrastructure.DocumentProcessing.Extraction;
using Lexora.Shared.Results;
using Lexora.TestKit.Samples;
using Microsoft.Extensions.Options;

namespace Lexora.Infrastructure.IntegrationTests.Processing;

public sealed class PdfTextExtractorTests
{
    [Fact]
    public async Task ExtractAsync_TextPages_ReturnsOneSectionPerPage()
    {
        var pdf = SamplePdf.WithPages(["Safety rules", "Wear gloves at all times."], ["Second page text."]);

        var result = await ExtractAsync(pdf);

        result.Value.PageCount.ShouldBe(2);
        result.Value.Sections.Select(section => section.PageNumber).ShouldBe([1, 2]);
        result.Value.Sections[0].Text.ShouldContain("Wear gloves at all times.");
        result.Value.Sections[1].Text.ShouldContain("Second page text.");
    }

    [Fact]
    public async Task ExtractAsync_StreamThatCannotSeek_IsBufferedFirst()
    {
        using var stream = new NonSeekableStream(SamplePdf.WithPages(["Buffered page."]));

        var result = await Extractor().ExtractAsync(stream, DocumentFormat.Pdf, TestContext.Current.CancellationToken);

        result.Value.Sections.Single().Text.ShouldContain("Buffered page.");
    }

    [Fact]
    public async Task ExtractAsync_MorePagesThanTheLimit_FailsWithThePageLimit() =>
        (await ExtractAsync(SamplePdf.Blank(3), maxPages: 2)).Notifications.ShouldBe([ExtractionFailures.PageLimit]);

    [Fact]
    public async Task ExtractAsync_EncryptedFile_FailsAsEncrypted() =>
        (await ExtractAsync(SamplePdf.Encrypted())).Notifications.ShouldBe([ExtractionFailures.Encrypted]);

    [Fact]
    public async Task ExtractAsync_BrokenFile_FailsAsUnreadable() =>
        (await ExtractAsync(Encoding.ASCII.GetBytes("%PDF-1.7\nthis is not a real pdf body"))).Notifications.ShouldBe([ExtractionFailures.Unreadable]);

    [Fact]
    public async Task ExtractAsync_PagesWithoutText_ReturnsEmptySections()
    {
        var result = await ExtractAsync(SamplePdf.Blank(2));

        result.Value.Sections.ShouldAllBe(section => section.Text.Length == 0);
    }

    private static PdfTextExtractor Extractor(int maxPages = 2_000) =>
        new(Options.Create(new DocumentProcessingOptions { MaxPages = maxPages }));

    private static async Task<Result<ExtractedDocument>> ExtractAsync(byte[] pdf, int maxPages = 2_000)
    {
        using var stream = new MemoryStream(pdf);

        return await Extractor(maxPages).ExtractAsync(stream, DocumentFormat.Pdf, TestContext.Current.CancellationToken);
    }
}
