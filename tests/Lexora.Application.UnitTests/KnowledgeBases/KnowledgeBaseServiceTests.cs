using Lexora.Application.KnowledgeBases;
using Lexora.Application.KnowledgeBases.ArchiveKnowledgeBase;
using Lexora.Application.KnowledgeBases.CreateKnowledgeBase;
using Lexora.Application.KnowledgeBases.Services;
using Lexora.Application.KnowledgeBases.UpdateKnowledgeBase;
using Lexora.Domain.KnowledgeBases;
using Lexora.Domain.KnowledgeBases.Events;
using Lexora.Shared.Results;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace Lexora.Application.UnitTests.KnowledgeBases;

public sealed class KnowledgeBaseServiceTests
{
    private readonly FakeTimeProvider _time = new(TestClock.Now);
    private readonly InMemoryKnowledgeBaseRepository _repository = new();
    private readonly FakeCurrentUser _currentUser = new FakeCurrentUser().SignIn(new UserBuilder().Build());
    private readonly KnowledgeBaseService _service;

    public KnowledgeBaseServiceTests() => _service = new KnowledgeBaseService(_repository, _currentUser, _time);

    [Fact]
    public async Task CreateAsync_WithoutChunking_UsesTheDefaultsAndRecordsTheCreator()
    {
        var result = await CreateAsync(new CreateKnowledgeBaseCommand("  Safety Procedures ", "Plant manuals", null, null));

        result.Value.Name.ShouldBe("Safety Procedures");
        result.Value.ChunkMaxTokens.ShouldBe(ChunkingSettings.DefaultMaxTokens);
        result.Value.ChunkOverlapTokens.ShouldBe(ChunkingSettings.DefaultOverlapTokens);
        result.Value.CreatedAt.ShouldBe(TestClock.Now);
        _repository.KnowledgeBases.Single().CreatedBy.ShouldBe(_currentUser.UserId);
    }

    [Fact]
    public async Task CreateAsync_NameTakenIgnoringCase_FailsAsDuplicate()
    {
        await CreateAsync(new CreateKnowledgeBaseCommand("Safety Procedures", null, null, null));

        var result = await CreateAsync(new CreateKnowledgeBaseCommand("safety procedures", null, null, null));

        result.Notifications.ShouldBe([KnowledgeBaseNotifications.NameDuplicate]);
        _repository.KnowledgeBases.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateAsync_OverlapAboveHalfOfMaxTokens_FailsAsInvalidChunking()
    {
        var result = await CreateAsync(new CreateKnowledgeBaseCommand("Safety Procedures", null, 400, 300));

        result.Notifications.Single().Key.ShouldBe(KnowledgeBaseRuleExtensions.InvalidChunkingKey);
        _repository.KnowledgeBases.ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_NewValues_RenamesDescribesAndRechunks()
    {
        var knowledgeBase = await AddAsync("Safety Procedures");
        _time.Advance(TimeSpan.FromHours(1));

        var result = await UpdateAsync(new UpdateKnowledgeBaseCommand(knowledgeBase.Id, "Safety Manuals", "Updated", 1000, 200));

        result.Value.Name.ShouldBe("Safety Manuals");
        result.Value.Description.ShouldBe("Updated");
        result.Value.ChunkMaxTokens.ShouldBe(1000);
        result.Value.ChunkOverlapTokens.ShouldBe(200);
        result.Value.UpdatedAt.ShouldBe(_time.GetUtcNow());
    }

    [Fact]
    public async Task UpdateAsync_ChunkingOmitted_KeepsTheCurrentSettings()
    {
        var knowledgeBase = await AddAsync("Safety Procedures", ChunkingSettings.Create(600, 60).Value);

        var result = await UpdateAsync(new UpdateKnowledgeBaseCommand(knowledgeBase.Id, "Safety Procedures", null, null, null));

        result.Value.ChunkMaxTokens.ShouldBe(600);
        result.Value.ChunkOverlapTokens.ShouldBe(60);
    }

    [Fact]
    public async Task UpdateAsync_SameNameWithDifferentCase_IsNotADuplicate()
    {
        var knowledgeBase = await AddAsync("Safety Procedures");

        (await UpdateAsync(new UpdateKnowledgeBaseCommand(knowledgeBase.Id, "SAFETY procedures", null, null, null))).Value.Name
            .ShouldBe("SAFETY procedures");
    }

    [Fact]
    public async Task UpdateAsync_NameOfAnotherBase_FailsAsDuplicateWithoutChanges()
    {
        await AddAsync("Quality Manuals");
        var knowledgeBase = await AddAsync("Safety Procedures");

        var result = await UpdateAsync(new UpdateKnowledgeBaseCommand(knowledgeBase.Id, "quality manuals", "Changed", null, null));

        result.Notifications.ShouldBe([KnowledgeBaseNotifications.NameDuplicate]);
        knowledgeBase.Name.Value.ShouldBe("Safety Procedures");
        knowledgeBase.Description.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_UnknownBase_FailsAsNotFound() =>
        (await UpdateAsync(new UpdateKnowledgeBaseCommand(Guid.CreateVersion7(), "Anything", null, null, null)))
            .Notifications.ShouldBe([KnowledgeBaseNotifications.NotFound]);

    [Fact]
    public async Task ArchiveAsync_ActiveBase_ArchivesItAndRaisesTheEvent()
    {
        var knowledgeBase = await AddAsync("Safety Procedures");

        var result = await _service.ArchiveAsync(new ArchiveKnowledgeBaseCommand(knowledgeBase.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        knowledgeBase.Status.ShouldBe(KnowledgeBaseStatus.Archived);
        knowledgeBase.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<KnowledgeBaseArchived>().KnowledgeBaseId.ShouldBe(knowledgeBase.Id);
    }

    [Fact]
    public async Task ArchiveAsync_UnknownBase_FailsAsNotFound() =>
        (await _service.ArchiveAsync(new ArchiveKnowledgeBaseCommand(Guid.CreateVersion7()), TestContext.Current.CancellationToken))
            .Notifications.ShouldBe([KnowledgeBaseNotifications.NotFound]);

    private Task<Result<CreateKnowledgeBaseResponse>> CreateAsync(CreateKnowledgeBaseCommand command) =>
        _service.CreateAsync(command, TestContext.Current.CancellationToken);

    private Task<Result<UpdateKnowledgeBaseResponse>> UpdateAsync(UpdateKnowledgeBaseCommand command) =>
        _service.UpdateAsync(command, TestContext.Current.CancellationToken);

    private async Task<KnowledgeBase> AddAsync(string name, ChunkingSettings? chunking = null)
    {
        var knowledgeBase = new KnowledgeBaseBuilder().WithName(name).WithChunking(chunking ?? ChunkingSettings.Default).Build();
        await _repository.AddAsync(knowledgeBase, TestContext.Current.CancellationToken);

        return knowledgeBase;
    }
}
