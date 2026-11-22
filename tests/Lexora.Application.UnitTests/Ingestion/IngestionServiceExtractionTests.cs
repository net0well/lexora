using Lexora.Application.Abstractions.Processing;
using Lexora.Application.Documents;
using Lexora.Application.Ingestion;
using Lexora.Application.Ingestion.Services;
using Lexora.Domain.Documents;
using Lexora.Domain.Documents.Events;
using Lexora.Shared.Results;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Lexora.Application.UnitTests.Ingestion;

public sealed class IngestionServiceExtractionTests
{
    private static readonly ExtractedDocument _extracted = new([new ExtractedSection("Intro", 1, null, "Hello")], PageCount: 1);

    private readonly FakeTimeProvider _time = new(TestClock.Now);
    private readonly InMemoryDocumentRepository _documents = new();
    private readonly InMemoryFileStorage _storage;
    private readonly IDocumentTextExtractor _extractor = Substitute.For<IDocumentTextExtractor>();
    private readonly IngestionService _service;

    public IngestionServiceExtractionTests()
    {
        _storage = new InMemoryFileStorage(_time);
        _extractor.ExtractAsync(Arg.Any<Stream>(), Arg.Any<DocumentFormat>(), Arg.Any<CancellationToken>()).Returns(_extracted);
        _service = new IngestionService(_documents, _extractor, _storage, _time, NullLogger<IngestionService>.Instance);
    }

    [Fact]
    public async Task ExtractAsync_UploadedDocument_StoresTheTextAndMovesToExtracted()
    {
        var document = await AddUploadedAsync();

        await _service.ExtractAsync(document.Id, 1, TestContext.Current.CancellationToken);

        var key = DocumentStorageKeys.ExtractedOf(document.KnowledgeBaseId, document.Id, 1);
        document.Status.ShouldBe(DocumentStatus.Extracted);
        document.ExtractedTextKey.ShouldBe(key);
        document.PageCount.ShouldBe(1);
        document.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<DocumentTextExtracted>();
        await using var stored = await _storage.OpenReadAsync(key, TestContext.Current.CancellationToken);
        (await ExtractedDocumentJson.DeserializeAsync(stored, TestContext.Current.CancellationToken)).Sections.ShouldBe(_extracted.Sections);
    }

    [Fact]
    public async Task ExtractAsync_ExtractorFailure_FailsTheDocumentWithTheReason()
    {
        _extractor.ExtractAsync(Arg.Any<Stream>(), Arg.Any<DocumentFormat>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ExtractedDocument>(ExtractionFailures.Encrypted));
        var document = await AddUploadedAsync();

        await _service.ExtractAsync(document.Id, 1, TestContext.Current.CancellationToken);

        document.Status.ShouldBe(DocumentStatus.Failed);
        document.FailureReason.ShouldBe("Encrypted PDF files are not supported.");
        document.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<DocumentFailed>();
    }

    [Fact]
    public async Task ExtractAsync_OriginalMissingFromStorage_FailsAsUnreadable()
    {
        var document = new DocumentBuilder().Build();
        document.ClearDomainEvents();
        await _documents.AddAsync(document, TestContext.Current.CancellationToken);

        await _service.ExtractAsync(document.Id, 1, TestContext.Current.CancellationToken);

        document.FailureReason.ShouldBe(ExtractionFailures.Unreadable.Message);
    }

    [Fact]
    public async Task ExtractAsync_MessageOfAnOlderVersion_IsIgnored()
    {
        var document = await AddUploadedAsync();
        document.Fail(1, "boom", TestClock.Now);
        document.RequestReprocess(TestClock.Now);
        document.ClearDomainEvents();

        await _service.ExtractAsync(document.Id, 1, TestContext.Current.CancellationToken);

        document.Status.ShouldBe(DocumentStatus.Uploaded);
        document.DomainEvents.ShouldBeEmpty();
        await _extractor.DidNotReceive().ExtractAsync(Arg.Any<Stream>(), Arg.Any<DocumentFormat>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExtractAsync_AlreadyExtracted_IsIgnored()
    {
        var document = await AddUploadedAsync();
        await _service.ExtractAsync(document.Id, 1, TestContext.Current.CancellationToken);
        document.ClearDomainEvents();

        await _service.ExtractAsync(document.Id, 1, TestContext.Current.CancellationToken);

        document.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_DeletedOrUnknownDocument_IsIgnored() =>
        await _service.ExtractAsync(Guid.CreateVersion7(), 1, TestContext.Current.CancellationToken);

    private async Task<Document> AddUploadedAsync()
    {
        var document = new DocumentBuilder().Build();
        document.ClearDomainEvents();
        await _documents.AddAsync(document, TestContext.Current.CancellationToken);
        using var original = new MemoryStream([1, 2, 3]);
        await _storage.SaveAsync(document.StorageKey, original, document.ContentType, TestContext.Current.CancellationToken);

        return document;
    }
}
