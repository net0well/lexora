using System.Security.Cryptography;
using System.Text;
using Lexora.Application.Common.Paging;
using Lexora.Application.Documents.Abstractions;
using Lexora.Application.Documents.ListDocumentChunks;
using Lexora.Application.Documents.ListDocuments;
using Lexora.Domain.Chunks;
using Lexora.Domain.Documents;
using Lexora.Domain.KnowledgeBases;
using Lexora.Domain.Users;
using Lexora.Infrastructure.Persistence.Queries;
using Lexora.TestKit;
using Lexora.TestKit.Builders;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class DocumentQueriesTests(DatabaseFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task GetByIdAsync_ExistingDocument_ProjectsItsDetails()
    {
        var (knowledgeBase, owner) = await SaveBaseAsync();
        var document = NewDocument(knowledgeBase, owner, "manual.pdf");
        await SaveAsync(document);

        await using var context = Database.CreateContext();
        var response = await new DocumentQueries(context).GetByIdAsync(document.Id, CancellationToken);

        response.ShouldNotBeNull();
        response.KnowledgeBaseId.ShouldBe(knowledgeBase.Id);
        response.FileName.ShouldBe("manual.pdf");
        response.Format.ShouldBe(DocumentFormat.Pdf);
        response.Status.ShouldBe(DocumentStatus.Uploaded);
        response.UploadedBy.ShouldBe(owner.Id);
    }

    [Fact]
    public async Task GetByIdAsync_DeletedDocument_ReturnsNull()
    {
        var (knowledgeBase, owner) = await SaveBaseAsync();
        var document = NewDocument(knowledgeBase, owner, "manual.pdf");
        document.Delete(TestClock.Now);
        await SaveAsync(document);

        await using var context = Database.CreateContext();

        (await new DocumentQueries(context).GetByIdAsync(document.Id, CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task GetStoredFileAsync_ExistingDocument_ReturnsWhereAndHowItIsStored()
    {
        var (knowledgeBase, owner) = await SaveBaseAsync();
        var document = NewDocument(knowledgeBase, owner, "manual.pdf");
        await SaveAsync(document);

        await using var context = Database.CreateContext();
        var file = await new DocumentQueries(context).GetStoredFileAsync(document.Id, CancellationToken);

        file.ShouldBe(new StoredDocumentFile(document.StorageKey, "manual.pdf", document.ContentType));
    }

    [Fact]
    public async Task ListAsync_Filters_ReturnTheNewestMatchingDocumentsFirst()
    {
        var (knowledgeBase, owner) = await SaveBaseAsync();
        var deleted = NewDocument(knowledgeBase, owner, "Old Manual.pdf", TestClock.Now.AddHours(-3));
        deleted.Delete(TestClock.Now);
        await SaveAsync(
            NewDocument(knowledgeBase, owner, "Safety Manual.pdf", TestClock.Now.AddHours(-2)),
            NewDocument(knowledgeBase, owner, "Quality manual.pdf", TestClock.Now.AddHours(-1)),
            NewDocument(knowledgeBase, owner, "Budget.xlsx", TestClock.Now),
            deleted);

        var page = await ListAsync(new ListDocumentsQuery(knowledgeBase.Id, DocumentStatus.Uploaded, "MANUAL"));

        page.ShouldNotBeNull().Items.Select(item => item.FileName).ShouldBe(["Quality manual.pdf", "Safety Manual.pdf"]);
        page.TotalItems.ShouldBe(2);
    }

    [Fact]
    public async Task ListAsync_DocumentsOfAnotherBase_AreNotListed()
    {
        var (knowledgeBase, owner) = await SaveBaseAsync();
        var other = new KnowledgeBaseBuilder().WithName("Other Base").CreatedBy(owner.Id).Build();
        await SaveAsync(other, NewDocument(other, owner, "other.pdf"));

        (await ListAsync(new ListDocumentsQuery(knowledgeBase.Id, null, null))).ShouldNotBeNull().TotalItems.ShouldBe(0);
    }

    [Fact]
    public async Task ListAsync_UnknownKnowledgeBase_ReturnsNull() =>
        (await ListAsync(new ListDocumentsQuery(Guid.CreateVersion7(), null, null))).ShouldBeNull();

    [Fact]
    public async Task ListChunksAsync_ReprocessedDocument_ListsOnlyTheCurrentVersionInOrder()
    {
        var (knowledgeBase, owner) = await SaveBaseAsync();
        var document = NewDocument(knowledgeBase, owner, "manual.pdf");
        await SaveAsync(
            document,
            NewChunk(document, version: 1, index: 1, page: 2),
            NewChunk(document, version: 1, index: 0, page: 1),
            NewChunk(document, version: 2, index: 0, page: 1));

        await using var context = Database.CreateContext();
        var page = await new DocumentQueries(context).ListChunksAsync(new ListDocumentChunksQuery(document.Id), CancellationToken);

        page.ShouldNotBeNull().Items.Select(chunk => (chunk.Index, chunk.PageNumber)).ShouldBe([(0, 1), (1, 2)]);
        page.Items[0].SectionTitle.ShouldBe("Section 0");
    }

    [Fact]
    public async Task ListChunksAsync_UnknownDocument_ReturnsNull()
    {
        await using var context = Database.CreateContext();

        (await new DocumentQueries(context).ListChunksAsync(new ListDocumentChunksQuery(Guid.CreateVersion7()), CancellationToken)).ShouldBeNull();
    }

    private async Task<(KnowledgeBase KnowledgeBase, User Owner)> SaveBaseAsync()
    {
        var owner = await SaveUserAsync();
        var knowledgeBase = new KnowledgeBaseBuilder().WithName("Safety Procedures").CreatedBy(owner.Id).Build();
        await SaveAsync(knowledgeBase);

        return (knowledgeBase, owner);
    }

    private async Task<PagedResponse<DocumentListItemResponse>?> ListAsync(ListDocumentsQuery query)
    {
        await using var context = Database.CreateContext();

        return await new DocumentQueries(context).ListAsync(query, CancellationToken);
    }

    private static Document NewDocument(KnowledgeBase knowledgeBase, User owner, string fileName, DateTimeOffset? uploadedAt = null)
    {
        var format = fileName.EndsWith(".xlsx", StringComparison.Ordinal) ? DocumentFormat.Xlsx : DocumentFormat.Pdf;

        return new DocumentBuilder()
            .InKnowledgeBase(knowledgeBase.Id)
            .UploadedBy(owner.Id)
            .WithFileName(fileName, format)
            .WithContentHash(ContentHash.FromHex(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(fileName)))))
            .UploadedAt(uploadedAt ?? TestClock.Now)
            .Build();
    }

    private static DocumentChunk NewChunk(Document document, int version, int index, int page) =>
        DocumentChunk.Create(
            document.KnowledgeBaseId,
            document.Id,
            version,
            index,
            $"Chunk {index} of version {version}.",
            12,
            ChunkLocation.Create(page, null, $"Section {index}", 0, 10));
}
