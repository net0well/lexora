using Lexora.Application.Conversations.ListConversations;
using Lexora.Domain.Conversations;
using Lexora.Infrastructure.Persistence.Queries;
using Lexora.TestKit;
using Lexora.TestKit.Builders;

namespace Lexora.Infrastructure.IntegrationTests.Persistence;

public sealed class ConversationQueriesTests(DatabaseFixture database) : DatabaseTest(database)
{
    [Fact]
    public async Task GetAsync_OwnConversation_ReturnsItsMessagesInOrderWithCitations()
    {
        var (owner, knowledgeBaseId) = await SaveOwnerWithBaseAsync();
        var conversation = Conversation.Start(knowledgeBaseId, owner, null, TestClock.Now).Value;
        conversation.AddUserMessage("Qual o raio de isolamento?", TestClock.Now);
        var citation = Citation.Create(1, Guid.CreateVersion7(), Guid.CreateVersion7(), "manual.pdf", 12, null, "Isole a área num raio de 50 metros.", 0.031);
        conversation.AddAssistantMessage("50 metros [1].", [citation], TokenUsage.Create(1874, 52), "ollama", "qwen2.5:7b", 2410, TestClock.Now.AddSeconds(3));
        await SaveAsync(conversation);

        await using var context = Database.CreateContext();
        var response = (await new ConversationQueries(context).GetAsync(conversation.Id, owner, CancellationToken)).ShouldNotBeNull();

        response.Title.ShouldBe("Qual o raio de isolamento?");
        response.Messages.Select(message => (message.Role, message.Content)).ShouldBe(
        [
            (MessageRole.User, "Qual o raio de isolamento?"),
            (MessageRole.Assistant, "50 metros [1]."),
        ]);
        var answer = response.Messages[1];
        (answer.Provider, answer.Model, answer.InputTokens, answer.OutputTokens, answer.LatencyMs).ShouldBe(("ollama", "qwen2.5:7b", 1874, 52, 2410L));
        answer.Citations.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            cited => cited.Index.ShouldBe(1),
            cited => cited.DocumentName.ShouldBe("manual.pdf"),
            cited => cited.PageNumber.ShouldBe(12),
            cited => cited.Excerpt.ShouldBe("Isole a área num raio de 50 metros."));
    }

    [Fact]
    public async Task GetAsync_ConversationOfAnotherUser_ReturnsNothing()
    {
        var (owner, knowledgeBaseId) = await SaveOwnerWithBaseAsync();
        var conversation = Conversation.Start(knowledgeBaseId, owner, "Privada", TestClock.Now).Value;
        await SaveAsync(conversation);

        await using var context = Database.CreateContext();

        (await new ConversationQueries(context).GetAsync(conversation.Id, Guid.CreateVersion7(), CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task ListAsync_OwnConversations_AreFilteredByBaseAndComeMostRecentFirst()
    {
        var (owner, knowledgeBaseId) = await SaveOwnerWithBaseAsync();
        var (otherOwner, otherBaseId) = await SaveOwnerWithBaseAsync();
        var older = Conversation.Start(knowledgeBaseId, owner, "Antiga", TestClock.Now).Value;
        var newer = Conversation.Start(knowledgeBaseId, owner, "Recente", TestClock.Now).Value;
        newer.AddUserMessage("Pergunta", TestClock.Now.AddMinutes(5));
        var otherBase = Conversation.Start(otherBaseId, owner, "Outra base", TestClock.Now.AddMinutes(10)).Value;
        var someoneElse = Conversation.Start(knowledgeBaseId, otherOwner, "De outra pessoa", TestClock.Now.AddMinutes(20)).Value;
        await SaveAsync(older, newer, otherBase, someoneElse);

        await using var context = Database.CreateContext();
        var queries = new ConversationQueries(context);

        var inBase = await queries.ListAsync(new ListConversationsQuery(knowledgeBaseId), owner, CancellationToken);
        var all = await queries.ListAsync(new ListConversationsQuery(null, Page: 1, PageSize: 2), owner, CancellationToken);

        inBase.Items.Select(item => (item.Title, item.MessageCount)).ShouldBe([("Recente", 1), ("Antiga", 0)]);
        (all.TotalItems, all.TotalPages).ShouldBe((3, 2));
        all.Items.Select(item => item.Title).ShouldBe(["Outra base", "Recente"]);
    }

    private async Task<(Guid OwnerId, Guid KnowledgeBaseId)> SaveOwnerWithBaseAsync()
    {
        var owner = await SaveUserAsync($"{Guid.CreateVersion7():N}@lexora.test");
        var knowledgeBase = new KnowledgeBaseBuilder().WithName($"Base {Guid.CreateVersion7():N}").CreatedBy(owner.Id).Build();
        await SaveAsync(knowledgeBase);

        return (owner.Id, knowledgeBase.Id);
    }
}
