using Lexora.Domain.Conversations;
using Lexora.Infrastructure.Persistence.Repositories;
using Lexora.TestKit;
using Lexora.TestKit.Builders;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class ConversationRepositoryTests(DatabaseFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task AddAsync_ConversationWithMessages_RoundTripsMessagesInOrderWithCitations()
    {
        var conversation = await NewConversationAsync();
        conversation.AddUserMessage("What are the payment terms?", TestClock.Now);
        var citation = Citation.Create(1, Guid.CreateVersion7(), Guid.CreateVersion7(), "contract.pdf", 4, null, "Payment is due within 30 days.", 0.031);
        conversation.AddAssistantMessage("Payment is due in 30 days [1].", [citation], TokenUsage.Create(1200, 85), "local", "qwen2.5:7b", 2900, TestClock.Now);

        await using (var context = Database.CreateContext())
        {
            await new ConversationRepository(context).AddAsync(conversation, CancellationToken);
            await context.SaveChangesAsync(CancellationToken);
        }

        await using var reader = Database.CreateContext();
        var loaded = await new ConversationRepository(reader).GetByIdAsync(conversation.Id, CancellationToken);

        loaded.ShouldNotBeNull();
        loaded.Title.ShouldBe("What are the payment terms?");
        loaded.IsOwnedBy(conversation.UserId).ShouldBeTrue();
        loaded.Messages.Select(message => message.Role).ShouldBe([MessageRole.User, MessageRole.Assistant]);
        var answer = loaded.Messages[1];
        answer.Content.ShouldBe("Payment is due in 30 days [1].");
        answer.Citations.ShouldBe([citation]);
        answer.Usage.ShouldBe(TokenUsage.Create(1200, 85));
        answer.Provider.ShouldBe("local");
        answer.Model.ShouldBe("qwen2.5:7b");
        answer.LatencyMs.ShouldBe(2900);
        loaded.Messages[0].Citations.ShouldBeEmpty();
        loaded.Messages[0].Usage.ShouldBe(TokenUsage.None);
    }

    [Fact]
    public async Task SaveChanges_MessageAddedToLoadedConversation_IsPersisted()
    {
        var conversation = await NewConversationAsync();
        conversation.AddUserMessage("First question", TestClock.Now);
        await SaveAsync(conversation);

        await using (var context = Database.CreateContext())
        {
            var loaded = await new ConversationRepository(context).GetByIdAsync(conversation.Id, CancellationToken);
            loaded.ShouldNotBeNull().AddUserMessage("Second question", TestClock.Now.AddMinutes(1));
            await context.SaveChangesAsync(CancellationToken);
        }

        await using var reader = Database.CreateContext();
        var reloaded = await new ConversationRepository(reader).GetByIdAsync(conversation.Id, CancellationToken);

        reloaded.ShouldNotBeNull().Messages.Select(message => message.Content).ShouldBe(["First question", "Second question"]);
        reloaded.UpdatedAt.ShouldBe(TestClock.Now.AddMinutes(1));
    }

    [Fact]
    public async Task GetByIdAsync_UnknownConversation_ReturnsNull()
    {
        await using var context = Database.CreateContext();

        (await new ConversationRepository(context).GetByIdAsync(Guid.CreateVersion7(), CancellationToken)).ShouldBeNull();
    }

    private async Task<Conversation> NewConversationAsync()
    {
        var owner = await SaveUserAsync();
        var knowledgeBase = new KnowledgeBaseBuilder().CreatedBy(owner.Id).Build();
        await SaveAsync(knowledgeBase);

        return Conversation.Start(knowledgeBase.Id, owner.Id, null, TestClock.Now).Value;
    }
}
