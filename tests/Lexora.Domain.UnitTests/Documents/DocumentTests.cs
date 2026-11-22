using Lexora.Domain.Documents;
using Lexora.Domain.Documents.Events;
using Lexora.Shared.Results;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Microsoft.Extensions.Time.Testing;

namespace Lexora.Domain.UnitTests.Documents;

public sealed class DocumentTests
{
    private const string ExtractedKey = "documents/v1/extracted.json";
    private const string EmbeddingModel = DocumentBuilder.DefaultEmbeddingModel;

    private static readonly Guid _knowledgeBaseId = Guid.CreateVersion7();
    private static readonly ContentHash _hash = ContentHash.FromHex("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");

    private readonly FakeTimeProvider _time = new(TestClock.Now);

    [Fact]
    public void Upload_ValidInput_StartsUploadedAtFirstVersion()
    {
        var id = Guid.CreateVersion7();
        var uploader = Guid.CreateVersion7();

        var document = Document.Upload(id, _knowledgeBaseId, "dir/Contract.pdf", DocumentFormat.Pdf, "application/pdf", 2048, _hash, "key", uploader, TestClock.Now).Value;

        document.Id.ShouldBe(id);
        document.KnowledgeBaseId.ShouldBe(_knowledgeBaseId);
        document.FileName.Value.ShouldBe("Contract.pdf");
        document.Format.ShouldBe(DocumentFormat.Pdf);
        document.ContentType.ShouldBe("application/pdf");
        document.SizeBytes.ShouldBe(2048);
        document.ContentHash.ShouldBe(_hash);
        document.StorageKey.ShouldBe("key");
        document.UploadedBy.ShouldBe(uploader);
        document.Status.ShouldBe(DocumentStatus.Uploaded);
        document.ProcessingVersion.ShouldBe(Document.InitialProcessingVersion);
        document.ChunkCount.ShouldBe(0);
        document.EmbeddedChunkCount.ShouldBe(0);
        document.UploadedAt.ShouldBe(TestClock.Now);
        document.UpdatedAt.ShouldBe(TestClock.Now);
        document.DomainEvents.ShouldBe([new DocumentUploaded(_knowledgeBaseId, id, 1, TestClock.Now)]);
    }

    [Fact]
    public void Upload_FileNameWithNothingUsable_FailsWithoutCreatingDocument() =>
        Document.Upload(Guid.CreateVersion7(), _knowledgeBaseId, "<>", DocumentFormat.Pdf, "application/pdf", 2048, _hash, "key", Guid.CreateVersion7(), TestClock.Now)
            .Notifications.Single().Key.ShouldBe("document.fileName.invalid");

    [Fact]
    public void Upload_EmptyFile_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Document.Upload(Guid.CreateVersion7(), _knowledgeBaseId, "a.pdf", DocumentFormat.Pdf, "application/pdf", 0, _hash, "key", Guid.CreateVersion7(), TestClock.Now));

    [Fact]
    public void Upload_EmptyIdentifier_Throws() =>
        Should.Throw<ArgumentException>(() =>
            Document.Upload(Guid.Empty, _knowledgeBaseId, "a.pdf", DocumentFormat.Pdf, "application/pdf", 10, _hash, "key", Guid.CreateVersion7(), TestClock.Now));

    [Fact]
    public void CompleteExtraction_UploadedDocument_MovesToExtracted()
    {
        var document = Build(DocumentStatus.Uploaded);
        Advance();

        var result = document.CompleteExtraction(1, 12, ExtractedKey, Now);

        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Extracted);
        document.PageCount.ShouldBe(12);
        document.ExtractedTextKey.ShouldBe(ExtractedKey);
        document.UpdatedAt.ShouldBe(Now);
        document.DomainEvents.ShouldBe([new DocumentTextExtracted(document.KnowledgeBaseId, document.Id, 1, Now)]);
    }

    [Theory]
    [InlineData(DocumentStatus.Extracted)]
    [InlineData(DocumentStatus.Chunked)]
    [InlineData(DocumentStatus.Indexed)]
    [InlineData(DocumentStatus.Failed)]
    [InlineData(DocumentStatus.Deleted)]
    public void CompleteExtraction_NotUploaded_FailsAsInvalidTransition(DocumentStatus status) =>
        ShouldBeRejected(Build(status), document => document.CompleteExtraction(1, 1, ExtractedKey, Now), "document.status.invalidTransition");

    [Fact]
    public void CompleteChunking_ExtractedDocument_MovesToChunked()
    {
        var document = Build(DocumentStatus.Extracted);
        Advance();

        var result = document.CompleteChunking(1, 40, Now);

        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Chunked);
        document.ChunkCount.ShouldBe(40);
        document.EmbeddedChunkCount.ShouldBe(0);
        document.HasPendingEmbeddings.ShouldBeTrue();
        document.DomainEvents.ShouldBe([new DocumentChunked(document.KnowledgeBaseId, document.Id, 1, Now)]);
    }

    [Theory]
    [InlineData(DocumentStatus.Uploaded)]
    [InlineData(DocumentStatus.Chunked)]
    [InlineData(DocumentStatus.Indexed)]
    [InlineData(DocumentStatus.Failed)]
    [InlineData(DocumentStatus.Deleted)]
    public void CompleteChunking_NotExtracted_FailsAsInvalidTransition(DocumentStatus status) =>
        ShouldBeRejected(Build(status), document => document.CompleteChunking(1, 4, Now), "document.status.invalidTransition");

    [Fact]
    public void RegisterEmbeddedBatch_ChunkedDocument_AccumulatesProgressWithoutEvents()
    {
        var document = new DocumentBuilder().WithChunkCount(4).InStatus(DocumentStatus.Chunked).Build();

        document.RegisterEmbeddedBatch(1, 2, Now).IsSuccess.ShouldBeTrue();
        document.RegisterEmbeddedBatch(1, 1, Now).IsSuccess.ShouldBeTrue();

        document.Status.ShouldBe(DocumentStatus.Chunked);
        document.EmbeddedChunkCount.ShouldBe(3);
        document.HasPendingEmbeddings.ShouldBeTrue();
        document.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RegisterEmbeddedBatch_MoreThanRemaining_Throws()
    {
        var document = new DocumentBuilder().WithChunkCount(4).InStatus(DocumentStatus.Chunked).Build();
        document.RegisterEmbeddedBatch(1, 3, Now);

        Should.Throw<ArgumentOutOfRangeException>(() => document.RegisterEmbeddedBatch(1, 2, Now));
    }

    [Theory]
    [InlineData(DocumentStatus.Uploaded)]
    [InlineData(DocumentStatus.Extracted)]
    [InlineData(DocumentStatus.Indexed)]
    [InlineData(DocumentStatus.Failed)]
    [InlineData(DocumentStatus.Deleted)]
    public void RegisterEmbeddedBatch_NotChunked_FailsAsInvalidTransition(DocumentStatus status) =>
        ShouldBeRejected(Build(status), document => document.RegisterEmbeddedBatch(1, 1, Now), "document.status.invalidTransition");

    [Fact]
    public void CompleteIndexing_AllChunksEmbedded_MovesToIndexed()
    {
        var document = new DocumentBuilder().WithChunkCount(4).InStatus(DocumentStatus.Chunked).Build();
        document.RegisterEmbeddedBatch(1, 4, Now);
        Advance();

        var result = document.CompleteIndexing(1, EmbeddingModel, Now);

        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Indexed);
        document.EmbeddingModel.ShouldBe(EmbeddingModel);
        document.IndexedAt.ShouldBe(Now);
        document.HasPendingEmbeddings.ShouldBeFalse();
        document.DomainEvents.ShouldBe([new DocumentIndexed(document.KnowledgeBaseId, document.Id, 1, Now)]);
    }

    [Fact]
    public void CompleteIndexing_WithPendingEmbeddings_FailsAsInvalidTransition()
    {
        var document = new DocumentBuilder().WithChunkCount(4).InStatus(DocumentStatus.Chunked).Build();
        document.RegisterEmbeddedBatch(1, 3, Now);

        ShouldBeRejected(document, candidate => candidate.CompleteIndexing(1, EmbeddingModel, Now), "document.status.invalidTransition");
    }

    [Theory]
    [InlineData(DocumentStatus.Uploaded)]
    [InlineData(DocumentStatus.Extracted)]
    [InlineData(DocumentStatus.Chunked)]
    public void Fail_DocumentInProcessing_MovesToFailedWithReason(DocumentStatus status)
    {
        var document = Build(status);
        Advance();

        var result = document.Fail(1, "Encrypted PDF files are not supported.", Now);

        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Failed);
        document.FailureReason.ShouldBe("Encrypted PDF files are not supported.");
        document.DomainEvents.ShouldBe([new DocumentFailed(document.KnowledgeBaseId, document.Id, 1, Now)]);
    }

    [Theory]
    [InlineData(DocumentStatus.Indexed)]
    [InlineData(DocumentStatus.Failed)]
    [InlineData(DocumentStatus.Deleted)]
    public void Fail_DocumentNotInProcessing_FailsAsInvalidTransition(DocumentStatus status) =>
        ShouldBeRejected(Build(status), document => document.Fail(1, "Processing timed out.", Now), "document.status.invalidTransition");

    [Theory]
    [InlineData(DocumentStatus.Indexed)]
    [InlineData(DocumentStatus.Failed)]
    public void RequestReprocess_FinishedDocument_RestartsAtNextVersion(DocumentStatus status)
    {
        var document = Build(status);
        Advance();

        var result = document.RequestReprocess(Now);

        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Uploaded);
        document.ProcessingVersion.ShouldBe(2);
        document.ExtractedTextKey.ShouldBeNull();
        document.PageCount.ShouldBeNull();
        document.ChunkCount.ShouldBe(0);
        document.EmbeddedChunkCount.ShouldBe(0);
        document.EmbeddingModel.ShouldBeNull();
        document.FailureReason.ShouldBeNull();
        document.IndexedAt.ShouldBeNull();
        document.DomainEvents.ShouldBe([new DocumentReprocessRequested(document.KnowledgeBaseId, document.Id, 2, Now)]);
    }

    [Theory]
    [InlineData(DocumentStatus.Uploaded)]
    [InlineData(DocumentStatus.Extracted)]
    [InlineData(DocumentStatus.Chunked)]
    [InlineData(DocumentStatus.Deleted)]
    public void RequestReprocess_DocumentNotFinished_FailsAsInvalidTransition(DocumentStatus status) =>
        ShouldBeRejected(Build(status), document => document.RequestReprocess(Now), "document.status.invalidTransition");

    [Fact]
    public void CompleteExtraction_MessageFromPreviousVersion_FailsAsStale()
    {
        var document = Build(DocumentStatus.Indexed);
        document.RequestReprocess(Now);
        document.ClearDomainEvents();

        ShouldBeRejected(document, candidate => candidate.CompleteExtraction(1, 3, ExtractedKey, Now), "document.processing.stale");
    }

    [Fact]
    public void Transition_PreviousVersionAndWrongStatus_ReportsStaleFirst()
    {
        var document = Build(DocumentStatus.Failed);
        document.RequestReprocess(Now);
        document.ClearDomainEvents();

        ShouldBeRejected(document, candidate => candidate.CompleteChunking(1, 4, Now), "document.processing.stale");
    }

    [Fact]
    public void CompleteExtraction_MessageFromCurrentVersionAfterReprocess_Succeeds()
    {
        var document = Build(DocumentStatus.Failed);
        document.RequestReprocess(Now);
        document.ClearDomainEvents();

        document.CompleteExtraction(2, 3, ExtractedKey, Now).IsSuccess.ShouldBeTrue();

        document.DomainEvents.ShouldBe([new DocumentTextExtracted(document.KnowledgeBaseId, document.Id, 2, Now)]);
    }

    [Theory]
    [InlineData(DocumentStatus.Uploaded)]
    [InlineData(DocumentStatus.Extracted)]
    [InlineData(DocumentStatus.Chunked)]
    [InlineData(DocumentStatus.Indexed)]
    [InlineData(DocumentStatus.Failed)]
    public void Delete_DocumentNotDeleted_MarksDeleted(DocumentStatus status)
    {
        var document = Build(status);
        Advance();

        var result = document.Delete(Now);

        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Deleted);
        document.DeletedAt.ShouldBe(Now);
        document.DomainEvents.ShouldBe([new DocumentDeleted(document.KnowledgeBaseId, document.Id, 1, Now)]);
    }

    [Fact]
    public void Delete_AlreadyDeleted_FailsAsInvalidTransition() =>
        ShouldBeRejected(Build(DocumentStatus.Deleted), document => document.Delete(Now), "document.status.invalidTransition");

    private DateTimeOffset Now => _time.GetUtcNow();

    private static Document Build(DocumentStatus status) => new DocumentBuilder().InKnowledgeBase(_knowledgeBaseId).InStatus(status).Build();

    private void ShouldBeRejected(Document document, Func<Document, Result> transition, string expectedKey)
    {
        var statusBefore = document.Status;
        var versionBefore = document.ProcessingVersion;
        var updatedBefore = document.UpdatedAt;
        Advance();

        var result = transition(document);

        result.Notifications.Single().ShouldSatisfyAllConditions(
            notification => notification.Key.ShouldBe(expectedKey),
            notification => notification.Type.ShouldBe(NotificationType.BusinessRule));
        document.Status.ShouldBe(statusBefore);
        document.ProcessingVersion.ShouldBe(versionBefore);
        document.UpdatedAt.ShouldBe(updatedBefore);
        document.DomainEvents.ShouldBeEmpty();
    }

    private void Advance() => _time.Advance(TimeSpan.FromMinutes(5));
}
