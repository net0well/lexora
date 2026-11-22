using Lexora.Application.Abstractions.Processing;
using Lexora.Application.Common.Options;
using Lexora.Application.Documents;
using Lexora.Application.Documents.DeleteDocument;
using Lexora.Application.Documents.ReprocessDocument;
using Lexora.Application.Documents.Services;
using Lexora.Domain.Documents;
using Lexora.Domain.Documents.Events;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Lexora.Application.UnitTests.Documents;

public sealed class DocumentServiceLifecycleTests
{
    private readonly FakeTimeProvider _time = new(TestClock.Now);
    private readonly InMemoryDocumentRepository _documents = new();
    private readonly DocumentService _service;

    public DocumentServiceLifecycleTests() =>
        _service = new DocumentService(
            _documents,
            new InMemoryKnowledgeBaseRepository(),
            Substitute.For<IDocumentTextExtractor>(),
            new InMemoryFileStorage(_time),
            new FakeCurrentUser().SignIn(new UserBuilder().Build()),
            Options.Create(new DocumentOptions()),
            _time);

    [Fact]
    public async Task DeleteAsync_ExistingDocument_MarksItDeletedAndRaisesTheEvent()
    {
        var document = await AddAsync(DocumentStatus.Indexed);
        _time.Advance(TimeSpan.FromMinutes(5));

        var result = await _service.DeleteAsync(new DeleteDocumentCommand(document.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        document.Status.ShouldBe(DocumentStatus.Deleted);
        document.DeletedAt.ShouldBe(_time.GetUtcNow());
        document.DomainEvents.OfType<DocumentDeleted>().ShouldHaveSingleItem().DocumentId.ShouldBe(document.Id);
    }

    [Fact]
    public async Task DeleteAsync_AlreadyDeletedDocument_FailsAsNotFound()
    {
        var document = await AddAsync(DocumentStatus.Indexed);
        await _service.DeleteAsync(new DeleteDocumentCommand(document.Id), TestContext.Current.CancellationToken);

        (await _service.DeleteAsync(new DeleteDocumentCommand(document.Id), TestContext.Current.CancellationToken))
            .Notifications.ShouldBe([DocumentNotifications.NotFound]);
    }

    [Theory]
    [InlineData(DocumentStatus.Indexed)]
    [InlineData(DocumentStatus.Failed)]
    public async Task ReprocessAsync_FinishedDocument_StartsANewVersion(DocumentStatus status)
    {
        var document = await AddAsync(status);

        var result = await _service.ReprocessAsync(new ReprocessDocumentCommand(document.Id), TestContext.Current.CancellationToken);

        result.Value.ShouldBe(new ReprocessDocumentResponse(document.Id, DocumentStatus.Uploaded, 2));
        document.DomainEvents.OfType<DocumentReprocessRequested>().ShouldHaveSingleItem().ProcessingVersion.ShouldBe(2);
    }

    [Fact]
    public async Task ReprocessAsync_DocumentStillProcessing_IsRejected()
    {
        var document = await AddAsync(DocumentStatus.Chunked);

        var result = await _service.ReprocessAsync(new ReprocessDocumentCommand(document.Id), TestContext.Current.CancellationToken);

        result.Notifications.Single().Key.ShouldBe("document.status.invalidTransition");
        document.ProcessingVersion.ShouldBe(Document.InitialProcessingVersion);
    }

    [Fact]
    public async Task ReprocessAsync_UnknownDocument_FailsAsNotFound() =>
        (await _service.ReprocessAsync(new ReprocessDocumentCommand(Guid.CreateVersion7()), TestContext.Current.CancellationToken))
            .Notifications.ShouldBe([DocumentNotifications.NotFound]);

    private async Task<Document> AddAsync(DocumentStatus status)
    {
        var document = new DocumentBuilder().InStatus(status).Build();
        document.ClearDomainEvents();
        await _documents.AddAsync(document, TestContext.Current.CancellationToken);

        return document;
    }
}
