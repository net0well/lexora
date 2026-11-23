using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Ai;
using Lexora.Application.Common.Options;
using Lexora.Application.Conversations;
using Lexora.Application.Conversations.AskQuestion;
using Lexora.Application.Conversations.Prompts;
using Lexora.Application.Conversations.RecordStreamedAnswer;
using Lexora.Application.Conversations.Services;
using Lexora.Application.Retrieval;
using Lexora.Application.Retrieval.Fusion;
using Lexora.Application.Retrieval.Services;
using Lexora.Domain.Conversations;
using Lexora.Domain.Users;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Lexora.Application.UnitTests.Conversations;

public sealed class AnswerServiceTests : IAsyncLifetime
{
    private static readonly SearchHit _radius = Hit("Em caso de vazamento, isole a área num raio de 50 metros.", page: 12);
    private static readonly SearchHit _gloves = Hit("Use luvas nitrílicas ao manusear solventes.", page: 3);

    private readonly InMemoryConversationRepository _conversations = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly IRetrievalService _retrieval = Substitute.For<IRetrievalService>();
    private readonly FakeChatModel _chat = new();
    private readonly IAiProfileCatalog _profiles = Substitute.For<IAiProfileCatalog>();
    private readonly FakeTimeProvider _time = new(TestClock.Now);
    private readonly AnswerService _service;
    private readonly Conversation _conversation;

    public AnswerServiceTests()
    {
        _currentUser.SignIn(new UserBuilder().WithRole(UserRole.Viewer).Build());
        _conversation = Conversation.Start(Guid.CreateVersion7(), _currentUser.UserId, null, TestClock.Now).Value;
        _retrieval.RetrieveAsync(default, default!, default, default, default).ReturnsForAnyArgs([_radius, _gloves]);
        _profiles.IsSelectable("claude").Returns(true);

        var options = Options.Create(new RagOptions { TopK = 8, HistoryMessages = 6 });
        _service = new AnswerService(
            _conversations,
            _currentUser,
            new AnswerContextBuilder(_retrieval, new RagPromptBuilder(new WordTokenCounter(), options), new FollowUpQuestionRewriter(_chat), options),
            _chat,
            new CitationParser(),
            _profiles,
            options,
            _time);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await _conversations.AddAsync(_conversation, CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task AnswerAsync_RelevantExcerpts_AnswersWithCitationsAndRecordsBothMessages()
    {
        var response = (await AskAsync("Qual o raio de isolamento?")).Value;

        response.ShouldSatisfyAllConditions(
            answer => answer.Answer.ShouldBe("Isole a área num raio de 50 metros [1]."),
            answer => answer.Grounded.ShouldBeTrue(),
            answer => (answer.Provider, answer.Model, answer.InputTokens, answer.OutputTokens, answer.LatencyMs).ShouldBe(("fake", "fake-chat", 120, 12, 250L)));
        response.Citations.ShouldHaveSingleItem().ShouldSatisfyAllConditions(
            citation => citation.Index.ShouldBe(1),
            citation => citation.PageNumber.ShouldBe(12),
            citation => citation.Excerpt.ShouldBe(_radius.Content));
        _conversation.Messages.Select(message => message.Role).ShouldBe([MessageRole.User, MessageRole.Assistant]);
        response.MessageId.ShouldBe(_conversation.Messages[1].Id);
        var request = _chat.Requests.ShouldHaveSingleItem();
        request.SystemPrompt.ShouldBe(RagPromptBuilder.SystemPrompt);
        request.Turns.ShouldHaveSingleItem().Content.ShouldEndWith("Question: Qual o raio de isolamento?");
        await _retrieval.Received(1).RetrieveAsync(_conversation.KnowledgeBaseId, "Qual o raio de isolamento?", SearchMode.Hybrid, 8, CancellationToken);
    }

    [Theory]
    [InlineData("Qual é o horário do refeitório?", NoContextAnswer.Portuguese)]
    [InlineData("What are the cafeteria opening hours?", NoContextAnswer.English)]
    [InlineData("¿Cuál es el horario del comedor?", NoContextAnswer.Spanish)]
    public async Task AnswerAsync_NothingFound_AnswersInTheQuestionLanguageWithoutCallingTheModel(string question, string expected)
    {
        _retrieval.RetrieveAsync(default, default!, default, default, CancellationToken).ReturnsForAnyArgs([]);

        var response = (await AskAsync(question)).Value;

        (response.Answer, response.Grounded, response.Provider, response.Model).ShouldBe((expected, false, AnswerService.NoContextProvider, AnswerService.NoContextModel));
        response.Citations.ShouldBeEmpty();
        _chat.Requests.ShouldBeEmpty();
        _conversation.Messages.Count.ShouldBe(2);
    }

    [Fact]
    public async Task AnswerAsync_AnswerWithoutValidCitations_IsNotGrounded()
    {
        _chat.DefaultAnswer = "Isole a área conforme o manual [7].";

        var response = (await AskAsync("Qual o raio de isolamento?")).Value;

        response.Grounded.ShouldBeFalse();
        response.Citations.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnswerAsync_FollowUp_SearchesWithTheRewrittenQuestionAndAnswersTheOriginalAfterTheEarlierTurns()
    {
        await AskAsync("Qual o raio de isolamento?");
        _time.Advance(TimeSpan.FromMinutes(1));
        _chat.Answers.Enqueue("Quais luvas usar em caso de vazamento químico?");

        await AskAsync("E quais luvas usar?", profile: "claude");

        await _retrieval.Received(1).RetrieveAsync(_conversation.KnowledgeBaseId, "Quais luvas usar em caso de vazamento químico?", SearchMode.Hybrid, 8, CancellationToken);
        _chat.Requests.Count.ShouldBe(3);
        _chat.Requests[1].ShouldSatisfyAllConditions(
            rewrite => rewrite.SystemPrompt.ShouldBe(FollowUpQuestionRewriter.SystemPrompt),
            rewrite => rewrite.MaxOutputTokens.ShouldBe(FollowUpQuestionRewriter.MaxOutputTokens));
        var request = _chat.Requests[^1];
        request.Turns[^1].Content.ShouldEndWith("Question: E quais luvas usar?");
        request.Profile.ShouldBe("claude");
        request.Turns.Select(turn => turn.Role).ShouldBe([ChatTurnRole.User, ChatTurnRole.Assistant, ChatTurnRole.User]);
        request.Turns[0].Content.ShouldBe("Qual o raio de isolamento?");
    }

    [Fact]
    public async Task AnswerAsync_ConversationOfAnotherUser_RespondsNotFoundWithoutAnswering()
    {
        _currentUser.SignIn(new UserBuilder().WithRole(UserRole.Viewer).Build());

        (await AskAsync("Qual o raio?")).Notifications.ShouldBe([ConversationNotifications.NotFound]);

        _conversation.Messages.ShouldBeEmpty();
        _chat.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnswerAsync_ProfileThatCannotBeChosen_IsRejectedBeforeAnything()
    {
        (await AskAsync("Qual o raio?", profile: "groq")).Notifications.ShouldBe([AiNotifications.ProfileNotSelectable]);

        _conversation.Messages.ShouldBeEmpty();
        await _retrieval.DidNotReceiveWithAnyArgs().RetrieveAsync(default, default!, default, default, CancellationToken);
    }

    [Fact]
    public async Task RecordAsync_StreamedAnswer_AddsTheQuestionAndTheAnswerInOrder()
    {
        var citation = Citation.Create(1, _radius.ChunkId, _radius.DocumentId, "manual.pdf", 12, null, "Isole a área.", 0.03);
        _time.Advance(TimeSpan.FromSeconds(3));

        var response = (await _service.RecordAsync(Recording(_currentUser.UserId, citation), CancellationToken)).Value;

        _conversation.Messages.Select(message => (message.Role, message.Content)).ShouldBe(
        [
            (MessageRole.User, "Qual o raio de isolamento?"),
            (MessageRole.Assistant, "Isole a área num raio de 50 metros [1]."),
        ]);
        _conversation.Messages[0].CreatedAt.ShouldBe(TestClock.Now);
        _conversation.Messages[1].ShouldSatisfyAllConditions(
            answer => answer.Id.ShouldBe(response.MessageId),
            answer => answer.Citations.ShouldBe([citation]),
            answer => answer.CreatedAt.ShouldBe(TestClock.Now.AddSeconds(3)));
    }

    [Fact]
    public async Task RecordAsync_ConversationOfAnotherUser_RecordsNothing()
    {
        (await _service.RecordAsync(Recording(Guid.CreateVersion7()), CancellationToken)).Notifications.ShouldBe([ConversationNotifications.NotFound]);

        _conversation.Messages.ShouldBeEmpty();
    }

    private RecordStreamedAnswerCommand Recording(Guid userId, params Citation[] citations) =>
        new(_conversation.Id, userId, "Qual o raio de isolamento?", TestClock.Now, "Isole a área num raio de 50 metros [1].", citations, TokenUsage.Create(120, 12), "fake", "fake-chat", 250);

    private Task<Lexora.Shared.Results.Result<AskQuestionResponse>> AskAsync(string question, string? profile = null) =>
        _service.AnswerAsync(new AskQuestionCommand(_conversation.Id, question, profile), CancellationToken);

    private static SearchHit Hit(string content, int page) =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), "manual.pdf", content, page, null, null, 0.03, SearchMatch.Both);
}
