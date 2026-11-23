using Lexora.Application.Conversations;
using Lexora.Application.Conversations.Services;
using Lexora.Application.Conversations.StartConversation;
using Lexora.Application.KnowledgeBases;
using Lexora.Domain.Conversations;
using Lexora.Domain.Users;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace Lexora.Application.UnitTests.Conversations;

public sealed class ConversationServiceTests
{
    private readonly InMemoryConversationRepository _conversations = new();
    private readonly InMemoryKnowledgeBaseRepository _knowledgeBases = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly ConversationService _service;

    public ConversationServiceTests()
    {
        _currentUser.SignIn(new UserBuilder().WithRole(UserRole.Viewer).Build());
        _service = new ConversationService(_conversations, _knowledgeBases, _currentUser, new FakeTimeProvider(TestClock.Now));
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task StartAsync_ExistingBase_StartsAConversationOwnedByTheCaller()
    {
        var knowledgeBase = new KnowledgeBaseBuilder().Build();
        await _knowledgeBases.AddAsync(knowledgeBase, CancellationToken);

        var response = (await _service.StartAsync(new StartConversationCommand(knowledgeBase.Id, " EPIs do laboratório "), CancellationToken)).Value;

        var conversation = _conversations.Conversations.ShouldHaveSingleItem();
        (response.Id, response.KnowledgeBaseId, response.Title, response.CreatedAt).ShouldBe((conversation.Id, knowledgeBase.Id, "EPIs do laboratório", TestClock.Now));
        conversation.IsOwnedBy(_currentUser.UserId).ShouldBeTrue();
    }

    [Fact]
    public async Task StartAsync_UnknownBase_RespondsNotFound()
    {
        var result = await _service.StartAsync(new StartConversationCommand(Guid.CreateVersion7(), null), CancellationToken);

        result.Notifications.ShouldBe([KnowledgeBaseNotifications.NotFound]);
        _conversations.Conversations.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_OwnConversation_RemovesIt()
    {
        var conversation = await SaveConversationAsync(_currentUser.UserId);

        (await _service.DeleteAsync(conversation.Id, CancellationToken)).IsSuccess.ShouldBeTrue();

        _conversations.Conversations.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_ConversationOfAnotherUserOrMissing_RespondsNotFoundAndKeepsIt()
    {
        var someoneElse = await SaveConversationAsync(Guid.CreateVersion7());

        (await _service.DeleteAsync(someoneElse.Id, CancellationToken)).Notifications.ShouldBe([ConversationNotifications.NotFound]);
        (await _service.DeleteAsync(Guid.CreateVersion7(), CancellationToken)).Notifications.ShouldBe([ConversationNotifications.NotFound]);

        _conversations.Conversations.ShouldHaveSingleItem();
    }

    private async Task<Conversation> SaveConversationAsync(Guid userId)
    {
        var conversation = Conversation.Start(Guid.CreateVersion7(), userId, "Dúvidas", TestClock.Now).Value;
        await _conversations.AddAsync(conversation, CancellationToken);

        return conversation;
    }
}
