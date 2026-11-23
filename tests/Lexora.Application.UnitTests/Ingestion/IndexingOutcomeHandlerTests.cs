using Lexora.Application.Documents.Abstractions;
using Lexora.Application.Documents.GetDocument;
using Lexora.Application.Ingestion.CompleteIndexing;
using Lexora.Domain.Documents;
using Lexora.Domain.Documents.Events;
using Lexora.TestKit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;

namespace Lexora.Application.UnitTests.Ingestion;

public sealed class IndexingOutcomeHandlerTests
{
    private readonly IDocumentQueries _queries = Substitute.For<IDocumentQueries>();

    [Fact]
    public async Task DocumentIndexed_KnownDocument_LogsTheIngestionDuration()
    {
        var document = Document(DocumentStatus.Indexed, failureReason: null);
        var logger = new FakeLogger<DocumentIndexedHandler>();

        await new DocumentIndexedHandler(_queries, logger)
            .Handle(new DocumentIndexed(document.KnowledgeBaseId, document.Id, 1, TestClock.Now.AddMinutes(3)), TestContext.Current.CancellationToken);

        var record = logger.Collector.LatestRecord;
        record.Level.ShouldBe(LogLevel.Information);
        record.Message.ShouldContain($"Indexed document {document.Id}");
        record.Message.ShouldContain("00:03:00");
    }

    [Fact]
    public async Task DocumentFailed_KnownDocument_LogsTheFormatAndReason()
    {
        var document = Document(DocumentStatus.Failed, failureReason: "Encrypted PDF files are not supported.");
        var logger = new FakeLogger<DocumentFailedHandler>();

        await new DocumentFailedHandler(_queries, logger)
            .Handle(new DocumentFailed(document.KnowledgeBaseId, document.Id, 1, TestClock.Now), TestContext.Current.CancellationToken);

        var record = logger.Collector.LatestRecord;
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("(Pdf) failed: Encrypted PDF files are not supported.");
    }

    [Fact]
    public async Task DocumentFailed_DocumentAlreadyDeleted_LogsNothing()
    {
        var logger = new FakeLogger<DocumentFailedHandler>();

        await new DocumentFailedHandler(_queries, logger)
            .Handle(new DocumentFailed(Guid.CreateVersion7(), Guid.CreateVersion7(), 1, TestClock.Now), TestContext.Current.CancellationToken);

        logger.Collector.Count.ShouldBe(0);
    }

    private GetDocumentResponse Document(DocumentStatus status, string? failureReason)
    {
        var document = new GetDocumentResponse(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "manual.pdf", DocumentFormat.Pdf, "application/pdf", 10, status, 1, 1, 4, 4,
            failureReason, Guid.CreateVersion7(), TestClock.Now, null, TestClock.Now);
        _queries.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        return document;
    }
}
