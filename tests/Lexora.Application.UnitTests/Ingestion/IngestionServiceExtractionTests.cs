using Lexora.Application.Abstractions.Processing;
using Lexora.Application.Documents;
using Lexora.Application.Ingestion;
using Lexora.Domain.Documents;
using Lexora.Domain.Documents.Events;
using Lexora.Shared.Results;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using NSubstitute;

namespace Lexora.Application.UnitTests.Ingestion;

public sealed class IngestionServiceExtractionTests
{
    private static readonly ExtractedDocument _extracted = new([new ExtractedSection("Intro", 1, null, "Hello")], PageCount: 1);

    private readonly IngestionHarness _harness = new();

    public IngestionServiceExtractionTests() =>
        _harness.Extractor.ExtractAsync(Arg.Any<Stream>(), Arg.Any<DocumentFormat>(), Arg.Any<CancellationToken>()).Returns(_extracted);

    [Fact]
    public async Task ExtractAsync_UploadedDocument_StoresTheTextAndMovesToExtracted()
    {
        var document = await _harness.AddUploadedAsync();

        await _harness.Service.ExtractAsync(document.Id, 1, TestContext.Current.CancellationToken);

        var key = DocumentStorageKeys.ExtractedOf(document.KnowledgeBaseId, document.Id, 1);
        document.Status.ShouldBe(DocumentStatus.Extracted);
        document.ExtractedTextKey.ShouldBe(key);
        document.PageCount.ShouldBe(1);
        document.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<DocumentTextExtracted>();
        await using var stored = await _harness.Storage.OpenReadAsync(key, TestContext.Current.CancellationToken);
        (await ExtractedDocumentJson.DeserializeAsync(stored, TestContext.Current.CancellationToken)).Sections.ShouldBe(_extracted.Sections);
    }

    [Fact]
    public async Task ExtractAsync_ExtractorFailure_FailsTheDocumentWithTheReason()
    {
        _harness.Extractor.ExtractAsync(Arg.Any<Stream>(), Arg.Any<DocumentFormat>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ExtractedDocument>(ExtractionFailures.Encrypted));
        var document = await _harness.AddUploadedAsync();

        await _harness.Service.ExtractAsync(document.Id, 1, TestContext.Current.CancellationToken);

        document.Status.ShouldBe(DocumentStatus.Failed);
        document.FailureReason.ShouldBe("Encrypted PDF files are not supported.");
        document.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<DocumentFailed>();
    }

    [Fact]
    public async Task ExtractAsync_OriginalMissingFromStorage_FailsAsUnreadable()
    {
        var document = new DocumentBuilder().Build();
        document.ClearDomainEvents();
        await _harness.Documents.AddAsync(document, TestContext.Current.CancellationToken);

        await _harness.Service.ExtractAsync(document.Id, 1, TestContext.Current.CancellationToken);

        document.FailureReason.ShouldBe(ExtractionFailures.Unreadable.Message);
    }

    [Fact]
    public async Task ExtractAsync_MessageOfAnOlderVersion_IsIgnored()
    {
        var document = await _harness.AddUploadedAsync();
        document.Fail(1, "boom", TestClock.Now);
        document.RequestReprocess(TestClock.Now);
        document.ClearDomainEvents();

        await _harness.Service.ExtractAsync(document.Id, 1, TestContext.Current.CancellationToken);

        document.Status.ShouldBe(DocumentStatus.Uploaded);
        document.DomainEvents.ShouldBeEmpty();
        await _harness.Extractor.DidNotReceive().ExtractAsync(Arg.Any<Stream>(), Arg.Any<DocumentFormat>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExtractAsync_AlreadyExtracted_IsIgnored()
    {
        var document = await _harness.AddUploadedAsync();
        await _harness.Service.ExtractAsync(document.Id, 1, TestContext.Current.CancellationToken);
        document.ClearDomainEvents();

        await _harness.Service.ExtractAsync(document.Id, 1, TestContext.Current.CancellationToken);

        document.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_DeletedOrUnknownDocument_IsIgnored() =>
        await _harness.Service.ExtractAsync(Guid.CreateVersion7(), 1, TestContext.Current.CancellationToken);
}
