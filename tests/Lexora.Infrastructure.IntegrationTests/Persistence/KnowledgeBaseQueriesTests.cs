using System.Security.Cryptography;
using System.Text;
using Lexora.Application.Common.Paging;
using Lexora.Application.KnowledgeBases.ListKnowledgeBases;
using Lexora.Domain.Documents;
using Lexora.Domain.KnowledgeBases;
using Lexora.Domain.Users;
using Lexora.Infrastructure.Persistence.Queries;
using Lexora.TestKit;
using Lexora.TestKit.Builders;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class KnowledgeBaseQueriesTests(DatabaseFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task GetByIdAsync_BaseWithDocuments_CountsOnlyDocumentsNotDeleted()
    {
        var owner = await SaveUserAsync();
        var knowledgeBase = new KnowledgeBaseBuilder()
            .WithName("Safety Procedures")
            .WithDescription("Plant manuals")
            .WithChunking(ChunkingSettings.Create(600, 60).Value)
            .CreatedBy(owner.Id)
            .Build();
        var deleted = NewDocument(knowledgeBase, owner, "removed.pdf");
        deleted.Delete(TestClock.Now).IsSuccess.ShouldBeTrue();
        await SaveAsync(knowledgeBase, NewDocument(knowledgeBase, owner, "a.pdf"), NewDocument(knowledgeBase, owner, "b.pdf"), deleted);

        await using var context = Database.CreateContext();
        var response = await new KnowledgeBaseQueries(context).GetByIdAsync(knowledgeBase.Id, CancellationToken);

        response.ShouldNotBeNull();
        response.Name.ShouldBe("Safety Procedures");
        response.Description.ShouldBe("Plant manuals");
        response.Status.ShouldBe(KnowledgeBaseStatus.Active);
        response.ChunkMaxTokens.ShouldBe(600);
        response.ChunkOverlapTokens.ShouldBe(60);
        response.DocumentCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownBase_ReturnsNull()
    {
        await using var context = Database.CreateContext();

        (await new KnowledgeBaseQueries(context).GetByIdAsync(Guid.CreateVersion7(), CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task ListAsync_StatusFilter_PagesTheMatchingBasesByName()
    {
        var owner = await SaveUserAsync();
        await SaveAsync(
            new KnowledgeBaseBuilder().WithName("Quality").CreatedBy(owner.Id).Build(),
            new KnowledgeBaseBuilder().WithName("Archive Room").CreatedBy(owner.Id).Archived().Build(),
            new KnowledgeBaseBuilder().WithName("HR Policies").CreatedBy(owner.Id).Build(),
            new KnowledgeBaseBuilder().WithName("Finance").CreatedBy(owner.Id).Build());

        var page = await ListAsync(new ListKnowledgeBasesQuery(KnowledgeBaseStatus.Active, Page: 1, PageSize: 2));

        page.Items.Select(item => item.Name).ShouldBe(["Finance", "HR Policies"]);
        page.TotalItems.ShouldBe(3);
        page.TotalPages.ShouldBe(2);
    }

    [Fact]
    public async Task ListAsync_NoFilter_IncludesArchivedBasesWithTheirDocumentCount()
    {
        var owner = await SaveUserAsync();
        var archived = new KnowledgeBaseBuilder().WithName("Archive Room").CreatedBy(owner.Id).Archived().Build();
        await SaveAsync(archived, NewDocument(archived, owner, "old.pdf"));

        var item = (await ListAsync(new ListKnowledgeBasesQuery(null))).Items.ShouldHaveSingleItem();

        item.Status.ShouldBe(KnowledgeBaseStatus.Archived);
        item.DocumentCount.ShouldBe(1);
    }

    private static Document NewDocument(KnowledgeBase knowledgeBase, User owner, string fileName) =>
        new DocumentBuilder()
            .InKnowledgeBase(knowledgeBase.Id)
            .UploadedBy(owner.Id)
            .WithFileName(fileName, DocumentFormat.Pdf)
            .WithContentHash(ContentHash.FromHex(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(fileName)))))
            .Build();

    private async Task<PagedResponse<KnowledgeBaseListItemResponse>> ListAsync(ListKnowledgeBasesQuery query)
    {
        await using var context = Database.CreateContext();

        return await new KnowledgeBaseQueries(context).ListAsync(query, CancellationToken);
    }
}
