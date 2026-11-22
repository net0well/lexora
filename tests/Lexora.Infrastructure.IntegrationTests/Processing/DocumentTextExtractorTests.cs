using Lexora.Application.Abstractions.Processing;
using Lexora.Domain.Documents;
using Lexora.Infrastructure.DocumentProcessing.Extraction;
using Lexora.Shared.Results;

namespace Lexora.Infrastructure.IntegrationTests.Processing;

public sealed class DocumentTextExtractorTests
{
    private static readonly ExtractedDocument _text = new([new ExtractedSection(null, 1, null, "Some text")], PageCount: 1);

    [Fact]
    public async Task ExtractAsync_FormatWithoutExtractor_FailsAsUnreadable()
    {
        var extractor = new DocumentTextExtractor([]);

        var result = await extractor.ExtractAsync(Stream.Null, DocumentFormat.Pdf, TestContext.Current.CancellationToken);

        result.Notifications.ShouldBe([ExtractionFailures.Unreadable]);
    }

    [Fact]
    public async Task ExtractAsync_OnlyBlankSections_FailsAsNoText()
    {
        var blank = new ExtractedDocument([new ExtractedSection(null, 1, null, "   "), new ExtractedSection(null, 2, null, "")], PageCount: 2);
        var extractor = new DocumentTextExtractor([new StubFormatExtractor(DocumentFormat.Pdf, blank)]);

        var result = await extractor.ExtractAsync(Stream.Null, DocumentFormat.Pdf, TestContext.Current.CancellationToken);

        result.Notifications.ShouldBe([ExtractionFailures.NoText]);
    }

    [Fact]
    public async Task ExtractAsync_TextFound_ReturnsTheExtractorResult()
    {
        var extractor = new DocumentTextExtractor([new StubFormatExtractor(DocumentFormat.Txt, _text)]);

        var result = await extractor.ExtractAsync(Stream.Null, DocumentFormat.Txt, TestContext.Current.CancellationToken);

        result.Value.ShouldBe(_text);
    }

    [Fact]
    public async Task ExtractAsync_ExtractorFailure_IsPassedThrough()
    {
        var extractor = new DocumentTextExtractor([new StubFormatExtractor(DocumentFormat.Pdf, Result.Failure<ExtractedDocument>(ExtractionFailures.Encrypted))]);

        (await extractor.ExtractAsync(Stream.Null, DocumentFormat.Pdf, TestContext.Current.CancellationToken))
            .Notifications.ShouldBe([ExtractionFailures.Encrypted]);
    }
}
