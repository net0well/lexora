using Lexora.Domain.Documents;
using Lexora.Domain.KnowledgeBases;
using Lexora.Infrastructure.Persistence.Repositories;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class DocumentRepositoryTests(DatabaseFixture database) : DatabaseTest(database)
{
    private static readonly ContentHash _hash = ContentHash.FromHex("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");

    [Fact]
    public async Task AddAsync_DocumentInProgress_RoundTripsEveryField()
    {
        var (ownerId, knowledgeBase) = await SaveKnowledgeBaseAsync();
        var document = NewDocument(ownerId, knowledgeBase.Id, DocumentStatus.Chunked, chunkCount: 4);
        document.RegisterEmbeddedBatch(1, 2, TestClock.Now);

        await using (var context = Database.CreateContext())
        {
            await new DocumentRepository(context).AddAsync(document, CancellationToken);
            await context.SaveChangesAsync(CancellationToken);
        }

        await using var reader = Database.CreateContext();
        var loaded = await new DocumentRepository(reader).GetByIdAsync(document.Id, CancellationToken);

        loaded.ShouldNotBeNull();
        loaded.KnowledgeBaseId.ShouldBe(knowledgeBase.Id);
        loaded.FileName.ShouldBe(document.FileName);
        loaded.Format.ShouldBe(DocumentFormat.Pdf);
        loaded.ContentType.ShouldBe(document.ContentType);
        loaded.SizeBytes.ShouldBe(document.SizeBytes);
        loaded.ContentHash.ShouldBe(document.ContentHash);
        loaded.StorageKey.ShouldBe(document.StorageKey);
        loaded.ExtractedTextKey.ShouldBe(document.ExtractedTextKey);
        loaded.Status.ShouldBe(DocumentStatus.Chunked);
        loaded.ProcessingVersion.ShouldBe(1);
        loaded.PageCount.ShouldBe(DocumentBuilder.DefaultPageCount);
        loaded.ChunkCount.ShouldBe(4);
        loaded.EmbeddedChunkCount.ShouldBe(2);
        loaded.UploadedBy.ShouldBe(ownerId);
        loaded.UploadedAt.ShouldBe(TestClock.Now);
        loaded.DeletedAt.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_DeletedDocument_ReturnsNull()
    {
        var (ownerId, knowledgeBase) = await SaveKnowledgeBaseAsync();
        var document = NewDocument(ownerId, knowledgeBase.Id, DocumentStatus.Deleted);
        await SaveAsync(document);

        await using var context = Database.CreateContext();

        (await new DocumentRepository(context).GetByIdAsync(document.Id, CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task ExistsWithHashAsync_ActiveDocumentWithHash_ReturnsTrueOnlyInItsBase()
    {
        var (ownerId, knowledgeBase) = await SaveKnowledgeBaseAsync("Contracts");
        var (_, otherBase) = await SaveKnowledgeBaseAsync("Policies", ownerId);
        await SaveAsync(NewDocument(ownerId, knowledgeBase.Id, DocumentStatus.Indexed, _hash));

        await using var context = Database.CreateContext();
        var repository = new DocumentRepository(context);

        (await repository.ExistsWithHashAsync(knowledgeBase.Id, _hash, CancellationToken)).ShouldBeTrue();
        (await repository.ExistsWithHashAsync(otherBase.Id, _hash, CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsWithHashAsync_OnlyADeletedDocumentHasTheHash_ReturnsFalse()
    {
        var (ownerId, knowledgeBase) = await SaveKnowledgeBaseAsync();
        await SaveAsync(NewDocument(ownerId, knowledgeBase.Id, DocumentStatus.Deleted, _hash));

        await using var context = Database.CreateContext();

        (await new DocumentRepository(context).ExistsWithHashAsync(knowledgeBase.Id, _hash, CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task SumActiveSizeAsync_ActiveAndDeletedDocuments_SumsOnlyActiveOnes()
    {
        var (ownerId, knowledgeBase) = await SaveKnowledgeBaseAsync("Contracts");
        var (_, otherBase) = await SaveKnowledgeBaseAsync("Policies", ownerId);
        await SaveAsync(
            new DocumentBuilder().InKnowledgeBase(knowledgeBase.Id).UploadedBy(ownerId).WithSize(1000).Build(),
            new DocumentBuilder().InKnowledgeBase(otherBase.Id).UploadedBy(ownerId).WithSize(250).Build(),
            new DocumentBuilder().InKnowledgeBase(knowledgeBase.Id).UploadedBy(ownerId).WithSize(9999).InStatus(DocumentStatus.Deleted).Build());

        await using var context = Database.CreateContext();

        (await new DocumentRepository(context).SumActiveSizeAsync(CancellationToken)).ShouldBe(1250);
    }

    [Fact]
    public async Task SaveChanges_SecondActiveDocumentWithSameHash_IsRejectedByTheDatabase()
    {
        var (ownerId, knowledgeBase) = await SaveKnowledgeBaseAsync();
        await SaveAsync(NewDocument(ownerId, knowledgeBase.Id, DocumentStatus.Indexed, _hash));

        await Should.ThrowAsync<DbUpdateException>(() => SaveAsync(NewDocument(ownerId, knowledgeBase.Id, DocumentStatus.Uploaded, _hash)));
    }

    [Fact]
    public async Task SaveChanges_SameHashAfterTheFirstWasDeleted_IsAccepted()
    {
        var (ownerId, knowledgeBase) = await SaveKnowledgeBaseAsync();
        await SaveAsync(NewDocument(ownerId, knowledgeBase.Id, DocumentStatus.Deleted, _hash));

        await SaveAsync(NewDocument(ownerId, knowledgeBase.Id, DocumentStatus.Uploaded, _hash));

        await using var context = Database.CreateContext();
        (await new DocumentRepository(context).ExistsWithHashAsync(knowledgeBase.Id, _hash, CancellationToken)).ShouldBeTrue();
    }

    [Fact]
    public async Task SaveChanges_ConcurrentUpdatesOfTheSameDocument_RejectsTheStaleOne()
    {
        var (ownerId, knowledgeBase) = await SaveKnowledgeBaseAsync();
        var document = NewDocument(ownerId, knowledgeBase.Id, DocumentStatus.Chunked, chunkCount: 4);
        await SaveAsync(document);

        await using var first = Database.CreateContext();
        await using var second = Database.CreateContext();
        var firstCopy = await new DocumentRepository(first).GetByIdAsync(document.Id, CancellationToken);
        var secondCopy = await new DocumentRepository(second).GetByIdAsync(document.Id, CancellationToken);

        firstCopy.ShouldNotBeNull().RegisterEmbeddedBatch(1, 1, TestClock.Now);
        await first.SaveChangesAsync(CancellationToken);
        secondCopy.ShouldNotBeNull().Delete(TestClock.Now);

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync(CancellationToken));
    }

    [Fact]
    public async Task ListStalledAsync_DocumentsStuckBeforeTheCutoff_AreReturnedOldestFirst()
    {
        var (ownerId, knowledgeBase) = await SaveKnowledgeBaseAsync();
        var older = new DocumentBuilder().InKnowledgeBase(knowledgeBase.Id).UploadedBy(ownerId).InStatus(DocumentStatus.Extracted).UploadedAt(TestClock.Now.AddHours(-3)).Build();
        var old = new DocumentBuilder().InKnowledgeBase(knowledgeBase.Id).UploadedBy(ownerId).InStatus(DocumentStatus.Uploaded).UploadedAt(TestClock.Now.AddHours(-2)).Build();
        var recent = new DocumentBuilder().InKnowledgeBase(knowledgeBase.Id).UploadedBy(ownerId).InStatus(DocumentStatus.Chunked).UploadedAt(TestClock.Now).Build();
        var indexed = new DocumentBuilder().InKnowledgeBase(knowledgeBase.Id).UploadedBy(ownerId).InStatus(DocumentStatus.Indexed).UploadedAt(TestClock.Now.AddDays(-1)).Build();
        await SaveAsync(older, old, recent, indexed);

        await using var context = Database.CreateContext();
        var stalled = await new DocumentRepository(context).ListStalledAsync(TestClock.Now.AddHours(-1), 10, CancellationToken);

        stalled.Select(document => document.Id).ShouldBe([older.Id, old.Id]);
    }

    [Fact]
    public async Task FindActiveIdsAsync_MixOfActiveDeletedAndUnknown_ReturnsOnlyTheActiveOnes()
    {
        var (ownerId, knowledgeBase) = await SaveKnowledgeBaseAsync();
        var active = NewDocument(ownerId, knowledgeBase.Id, DocumentStatus.Indexed);
        var deleted = NewDocument(ownerId, knowledgeBase.Id, DocumentStatus.Indexed);
        deleted.Delete(TestClock.Now);
        await SaveAsync(active, deleted);

        await using var context = Database.CreateContext();
        var ids = await new DocumentRepository(context).FindActiveIdsAsync([active.Id, deleted.Id, Guid.CreateVersion7()], CancellationToken);

        ids.ShouldBe([active.Id]);
    }

    private async Task<(Guid OwnerId, KnowledgeBase KnowledgeBase)> SaveKnowledgeBaseAsync(string name = "Contracts", Guid? ownerId = null)
    {
        var owner = ownerId ?? (await SaveUserAsync()).Id;
        var knowledgeBase = new KnowledgeBaseBuilder().WithName(name).CreatedBy(owner).Build();
        await SaveAsync(knowledgeBase);

        return (owner, knowledgeBase);
    }

    private static Document NewDocument(Guid ownerId, Guid knowledgeBaseId, DocumentStatus status, ContentHash? hash = null, int chunkCount = 4)
    {
        var builder = new DocumentBuilder().InKnowledgeBase(knowledgeBaseId).UploadedBy(ownerId).InStatus(status).WithChunkCount(chunkCount);

        return (hash is null ? builder : builder.WithContentHash(hash)).Build();
    }
}
