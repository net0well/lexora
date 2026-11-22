using Lexora.Domain.KnowledgeBases;
using Lexora.Domain.KnowledgeBases.Events;
using Lexora.Shared.Results;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Microsoft.Extensions.Time.Testing;

namespace Lexora.Domain.UnitTests.KnowledgeBases;

public sealed class KnowledgeBaseTests
{
    private static readonly Guid _adminId = Guid.CreateVersion7();

    private readonly FakeTimeProvider _time = new(TestClock.Now);

    [Fact]
    public void Create_ValidInput_CreatesActiveBase()
    {
        var chunking = ChunkingSettings.Create(1000, 200).Value;

        var result = KnowledgeBase.Create("  Contracts ", "  Signed supplier contracts ", chunking, _adminId, TestClock.Now);

        result.IsSuccess.ShouldBeTrue();
        var knowledgeBase = result.Value;
        knowledgeBase.Name.Value.ShouldBe("Contracts");
        knowledgeBase.Description.ShouldBe("Signed supplier contracts");
        knowledgeBase.Chunking.ShouldBe(chunking);
        knowledgeBase.Status.ShouldBe(KnowledgeBaseStatus.Active);
        knowledgeBase.IsArchived.ShouldBeFalse();
        knowledgeBase.CreatedBy.ShouldBe(_adminId);
        knowledgeBase.CreatedAt.ShouldBe(TestClock.Now);
        knowledgeBase.UpdatedAt.ShouldBe(TestClock.Now);
        knowledgeBase.DomainEvents.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankDescription_StoresNull(string? description) =>
        KnowledgeBase.Create("Contracts", description, ChunkingSettings.Default, _adminId, TestClock.Now).Value.Description.ShouldBeNull();

    [Fact]
    public void Create_DescriptionAtLimit_Succeeds() =>
        KnowledgeBase.Create("Contracts", new string('d', KnowledgeBase.DescriptionMaxLength), ChunkingSettings.Default, _adminId, TestClock.Now)
            .IsSuccess.ShouldBeTrue();

    [Fact]
    public void Create_InvalidNameAndDescription_ReportsBoth() =>
        KnowledgeBase.Create("ab", new string('d', KnowledgeBase.DescriptionMaxLength + 1), ChunkingSettings.Default, _adminId, TestClock.Now)
            .Notifications.Select(notification => notification.Key)
            .ShouldBe(["knowledgeBase.name.tooShort", "knowledgeBase.description.tooLong"]);

    [Fact]
    public void Create_EmptyCreator_Throws() =>
        Should.Throw<ArgumentException>(() => KnowledgeBase.Create("Contracts", null, ChunkingSettings.Default, Guid.Empty, TestClock.Now));

    [Fact]
    public void Create_NullChunking_Throws() =>
        Should.Throw<ArgumentNullException>(() => KnowledgeBase.Create("Contracts", null, null!, _adminId, TestClock.Now));

    [Fact]
    public void Rename_ValidName_ChangesNameAndUpdateTime()
    {
        var knowledgeBase = new KnowledgeBaseBuilder().Build();
        _time.Advance(TimeSpan.FromHours(1));

        var result = knowledgeBase.Rename(" Policies ", _time.GetUtcNow());

        result.IsSuccess.ShouldBeTrue();
        knowledgeBase.Name.Value.ShouldBe("Policies");
        knowledgeBase.UpdatedAt.ShouldBe(_time.GetUtcNow());
    }

    [Fact]
    public void Rename_InvalidName_KeepsCurrentName()
    {
        var knowledgeBase = new KnowledgeBaseBuilder().WithName("Contracts").Build();
        _time.Advance(TimeSpan.FromHours(1));

        var result = knowledgeBase.Rename("", _time.GetUtcNow());

        result.Notifications.Single().Key.ShouldBe("knowledgeBase.name.required");
        knowledgeBase.Name.Value.ShouldBe("Contracts");
        knowledgeBase.UpdatedAt.ShouldBe(TestClock.Now);
    }

    [Fact]
    public void UpdateDescription_Blank_ClearsDescription()
    {
        var knowledgeBase = new KnowledgeBaseBuilder().WithDescription("Old").Build();

        knowledgeBase.UpdateDescription("  ", _time.GetUtcNow()).IsSuccess.ShouldBeTrue();

        knowledgeBase.Description.ShouldBeNull();
    }

    [Fact]
    public void UpdateDescription_OverLimit_KeepsCurrentDescription()
    {
        var knowledgeBase = new KnowledgeBaseBuilder().WithDescription("Old").Build();

        var result = knowledgeBase.UpdateDescription(new string('d', KnowledgeBase.DescriptionMaxLength + 1), _time.GetUtcNow());

        result.Notifications.Single().Key.ShouldBe("knowledgeBase.description.tooLong");
        knowledgeBase.Description.ShouldBe("Old");
    }

    [Fact]
    public void ChangeChunking_NewSettings_ReplacesThem()
    {
        var knowledgeBase = new KnowledgeBaseBuilder().Build();
        var chunking = ChunkingSettings.Create(400, 50).Value;
        _time.Advance(TimeSpan.FromHours(1));

        knowledgeBase.ChangeChunking(chunking, _time.GetUtcNow());

        knowledgeBase.Chunking.ShouldBe(chunking);
        knowledgeBase.UpdatedAt.ShouldBe(_time.GetUtcNow());
    }

    [Fact]
    public void Archive_ActiveBase_ArchivesAndRaisesEvent()
    {
        var knowledgeBase = new KnowledgeBaseBuilder().Build();
        _time.Advance(TimeSpan.FromDays(1));

        knowledgeBase.Archive(_time.GetUtcNow());

        knowledgeBase.Status.ShouldBe(KnowledgeBaseStatus.Archived);
        knowledgeBase.IsArchived.ShouldBeTrue();
        knowledgeBase.UpdatedAt.ShouldBe(_time.GetUtcNow());
        knowledgeBase.DomainEvents.ShouldBe([new KnowledgeBaseArchived(knowledgeBase.Id, _time.GetUtcNow())]);
    }

    [Fact]
    public void Archive_AlreadyArchived_DoesNotRaiseAgain()
    {
        var knowledgeBase = new KnowledgeBaseBuilder().Archived().Build();

        knowledgeBase.Archive(_time.GetUtcNow());

        knowledgeBase.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void EnsureAcceptsUploads_ActiveBase_Succeeds() => new KnowledgeBaseBuilder().Build().EnsureAcceptsUploads().IsSuccess.ShouldBeTrue();

    [Fact]
    public void EnsureAcceptsUploads_ArchivedBase_FailsAsBusinessRule() =>
        new KnowledgeBaseBuilder().Archived().Build().EnsureAcceptsUploads().Notifications.Single().ShouldSatisfyAllConditions(
            notification => notification.Key.ShouldBe("knowledgeBase.archived"),
            notification => notification.Type.ShouldBe(NotificationType.BusinessRule));
}
