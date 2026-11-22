using Lexora.Application.Common.Paging;
using Lexora.Application.Common.Validation;
using Lexora.Application.Documents;
using Lexora.Application.Documents.Abstractions;
using Lexora.Application.Documents.GetDocument;
using Lexora.Application.Documents.ListDocumentChunks;
using Lexora.Application.Documents.ListDocuments;
using Lexora.Application.KnowledgeBases;
using Lexora.Domain.Documents;
using Lexora.TestKit;
using NSubstitute;

namespace Lexora.Application.UnitTests.Documents;

public sealed class DocumentQueryHandlerTests
{
    private readonly IDocumentQueries _queries = Substitute.For<IDocumentQueries>();

    [Fact]
    public async Task GetDocument_Existing_ReturnsIt()
    {
        var document = new GetDocumentResponse(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "manual.pdf", DocumentFormat.Pdf, "application/pdf", 10,
            DocumentStatus.Uploaded, 1, null, 0, 0, null, Guid.CreateVersion7(), TestClock.Now, null, TestClock.Now);
        _queries.GetByIdAsync(document.Id, Arg.Any<CancellationToken>()).Returns(document);

        (await GetDocumentHandler.Handle(new GetDocumentQuery(document.Id), _queries, TestContext.Current.CancellationToken)).Value.ShouldBe(document);
    }

    [Fact]
    public async Task GetDocument_UnknownOrDeleted_FailsAsNotFound() =>
        (await GetDocumentHandler.Handle(new GetDocumentQuery(Guid.CreateVersion7()), _queries, TestContext.Current.CancellationToken))
            .Notifications.ShouldBe([DocumentNotifications.NotFound]);

    [Fact]
    public async Task ListDocuments_UnknownKnowledgeBase_FailsAsNotFound() =>
        (await ListDocumentsHandler.Handle(new ListDocumentsQuery(Guid.CreateVersion7(), null, null), _queries, TestContext.Current.CancellationToken))
            .Notifications.ShouldBe([KnowledgeBaseNotifications.NotFound]);

    [Fact]
    public async Task ListDocuments_ExistingKnowledgeBase_ReturnsThePage()
    {
        var query = new ListDocumentsQuery(Guid.CreateVersion7(), DocumentStatus.Indexed, "manual");
        var page = PagedResponse.Create<DocumentListItemResponse>([], query.Paging, totalItems: 0);
        _queries.ListAsync(query, Arg.Any<CancellationToken>()).Returns(page);

        (await ListDocumentsHandler.Handle(query, _queries, TestContext.Current.CancellationToken)).Value.ShouldBe(page);
    }

    [Fact]
    public async Task ListDocumentChunks_UnknownDocument_FailsAsNotFound() =>
        (await ListDocumentChunksHandler.Handle(new ListDocumentChunksQuery(Guid.CreateVersion7()), _queries, TestContext.Current.CancellationToken))
            .Notifications.ShouldBe([DocumentNotifications.NotFound]);

    [Theory]
    [InlineData(DocumentStatus.Deleted)]
    [InlineData((DocumentStatus)42)]
    public void ListDocumentsValidator_DeletedOrUndefinedStatus_IsInvalid(DocumentStatus status) =>
        new ListDocumentsValidator().Validate(new ListDocumentsQuery(Guid.CreateVersion7(), status, null))
            .Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("document.status.invalid");

    [Fact]
    public void ListDocumentsValidator_LongSearch_IsInvalid() =>
        new ListDocumentsValidator().Validate(new ListDocumentsQuery(Guid.CreateVersion7(), null, new string('a', ListDocumentsValidator.MaxSearchLength + 1)))
            .Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("document.search.tooLong");

    [Fact]
    public void ListDocumentChunksValidator_PageSizeAboveTheLimit_IsInvalid() =>
        new ListDocumentChunksValidator().Validate(new ListDocumentChunksQuery(Guid.CreateVersion7(), 1, 101))
            .Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe(PagingRuleExtensions.InvalidPageSizeKey);
}
