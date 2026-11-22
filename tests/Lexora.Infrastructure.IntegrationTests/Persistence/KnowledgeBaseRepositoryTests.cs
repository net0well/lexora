using Lexora.Domain.KnowledgeBases;
using Lexora.Infrastructure.Persistence.Repositories;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class KnowledgeBaseRepositoryTests(DatabaseFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task AddAsync_NewBase_RoundTripsNameDescriptionAndChunking()
    {
        var owner = await SaveUserAsync();
        var knowledgeBase = new KnowledgeBaseBuilder()
            .WithName("Contracts")
            .WithDescription("Signed supplier contracts")
            .WithChunking(ChunkingSettings.Create(1000, 200).Value)
            .CreatedBy(owner.Id)
            .Build();

        await using (var context = Database.CreateContext())
        {
            await new KnowledgeBaseRepository(context).AddAsync(knowledgeBase, CancellationToken);
            await context.SaveChangesAsync(CancellationToken);
        }

        await using var reader = Database.CreateContext();
        var loaded = await new KnowledgeBaseRepository(reader).GetByIdAsync(knowledgeBase.Id, CancellationToken);

        loaded.ShouldNotBeNull();
        loaded.Name.Value.ShouldBe("Contracts");
        loaded.Description.ShouldBe("Signed supplier contracts");
        loaded.Chunking.ShouldBe(ChunkingSettings.Create(1000, 200).Value);
        loaded.Status.ShouldBe(KnowledgeBaseStatus.Active);
        loaded.CreatedBy.ShouldBe(owner.Id);
        loaded.CreatedAt.ShouldBe(TestClock.Now);
    }

    [Fact]
    public async Task GetByIdAsync_ArchivedBase_KeepsArchivedStatus()
    {
        var owner = await SaveUserAsync();
        var knowledgeBase = new KnowledgeBaseBuilder().CreatedBy(owner.Id).Archived().Build();
        await SaveAsync(knowledgeBase);

        await using var context = Database.CreateContext();
        var loaded = await new KnowledgeBaseRepository(context).GetByIdAsync(knowledgeBase.Id, CancellationToken);

        loaded.ShouldNotBeNull().IsArchived.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Contracts")]
    [InlineData("CONTRACTS")]
    [InlineData("  contracts ")]
    public async Task ExistsByNameAsync_SameNameInAnyCase_ReturnsTrue(string name)
    {
        var owner = await SaveUserAsync();
        await SaveAsync(new KnowledgeBaseBuilder().WithName("Contracts").CreatedBy(owner.Id).Build());

        await using var context = Database.CreateContext();

        (await new KnowledgeBaseRepository(context).ExistsByNameAsync(name, CancellationToken)).ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsByNameAsync_OtherName_ReturnsFalse()
    {
        var owner = await SaveUserAsync();
        await SaveAsync(new KnowledgeBaseBuilder().WithName("Contracts").CreatedBy(owner.Id).Build());

        await using var context = Database.CreateContext();

        (await new KnowledgeBaseRepository(context).ExistsByNameAsync("Policies", CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task SaveChanges_SameNameInOtherCase_IsRejectedByTheDatabase()
    {
        var owner = await SaveUserAsync();
        await SaveAsync(new KnowledgeBaseBuilder().WithName("Contracts").CreatedBy(owner.Id).Build());

        await Should.ThrowAsync<DbUpdateException>(() =>
            SaveAsync(new KnowledgeBaseBuilder().WithName("CONTRACTS").CreatedBy(owner.Id).Build()));
    }
}
