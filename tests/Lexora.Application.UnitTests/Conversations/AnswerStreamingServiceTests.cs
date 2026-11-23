using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Common.Options;
using Lexora.Application.Conversations;
using Lexora.Application.Conversations.AskQuestion;
using Lexora.Application.Conversations.Prompts;
using Lexora.Application.Conversations.RecordStreamedAnswer;
using Lexora.Application.Conversations.Services;
using Lexora.Application.Conversations.Streaming;
using Lexora.Application.Retrieval;
using Lexora.Application.Retrieval.Fusion;
using Lexora.Application.Retrieval.Services;
using Lexora.Domain.Conversations;
using Lexora.Domain.Users;
using Lexora.Shared.Results;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Wolverine;

namespace Lexora.Application.UnitTests.Conversations;

public sealed class AnswerStreamingServiceTests
{
    private static readonly SearchHit _radius =
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), "manual.pdf", "Isole a área num raio de 50 metros.", 12, null, null, 0.03, SearchMatch.Both);

    private readonly InMemoryConversationRepository _conversations = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly IRetrievalService _retrieval = Substitute.For<IRetrievalService>();
    private readonly FakeChatModel _chat = new();
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly List<RecordStreamedAnswerCommand> _recorded = [];
    private readonly Guid _messageId = Guid.CreateVersion7();
    private readonly AnswerStreamingService _service;
    private readonly Conversation _conversation;

    public AnswerStreamingServiceTests()
    {
        _currentUser.SignIn(new UserBuilder().WithRole(UserRole.Viewer).Build());
        _conversation = Conversation.Start(Guid.CreateVersion7(), _currentUser.UserId, null, TestClock.Now).Value;
        _ = _conversations.AddAsync(_conversation, CancellationToken.None);
        _retrieval.RetrieveAsync(default, default!, default, default, default).ReturnsForAnyArgs([_radius]);
        _bus.InvokeAsync<Result<RecordStreamedAnswerResponse>>(Arg.Do<object>(message => _recorded.Add((RecordStreamedAnswerCommand)message)), Arg.Any<CancellationToken>(), Arg.Any<TimeSpan?>())
            .Returns(Result.Success(new RecordStreamedAnswerResponse(_messageId)));

        var options = Options.Create(new RagOptions());
        _service = new AnswerStreamingService(
            _conversations,
            _currentUser,
            new AnswerContextBuilder(_retrieval, new RagPromptBuilder(new WordTokenCounter(), options), new FollowUpQuestionRewriter(_chat), options),
            _chat,
            new CitationParser(),
            Substitute.For<IAiProfileCatalog>(),
            _bus,
            options,
            new FakeTimeProvider(TestClock.Now));
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task StreamAsync_RelevantExcerpts_SendsTheTextThenTheCitationsThenRecordsAndFinishes()
    {
        var events = await CollectAsync("Qual o raio de isolamento?");

        events.OfType<AnswerDeltaEvent>().Count().ShouldBeGreaterThan(1);
        string.Concat(events.OfType<AnswerDeltaEvent>().Select(delta => delta.Text)).ShouldBe("Isole a área num raio de 50 metros [1].");
        events.SkipWhile(item => item is AnswerDeltaEvent).Select(item => item.GetType()).ShouldBe([typeof(AnswerCitationsEvent), typeof(AnswerDoneEvent)]);
        var citations = events.OfType<AnswerCitationsEvent>().Single();
        citations.Grounded.ShouldBeTrue();
        citations.Citations.ShouldHaveSingleItem().PageNumber.ShouldBe(12);
        events.OfType<AnswerDoneEvent>().Single().ShouldBe(new AnswerDoneEvent(_conversation.Id, _messageId, "fake", "fake-chat", 120, 12, 250));
        var recorded = _recorded.ShouldHaveSingleItem();
        (recorded.UserId, recorded.Question, recorded.Answer, recorded.AskedAt).ShouldBe((_currentUser.UserId, "Qual o raio de isolamento?", "Isole a área num raio de 50 metros [1].", TestClock.Now));
        recorded.Citations.ShouldHaveSingleItem().Index.ShouldBe(1);
        _conversation.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task StreamAsync_NothingFound_SendsTheFixedAnswerWithoutCallingTheModel()
    {
        _retrieval.RetrieveAsync(default, default!, default, default, CancellationToken).ReturnsForAnyArgs([]);

        var events = await CollectAsync("Qual o horário do refeitório?");

        events.OfType<AnswerDeltaEvent>().ShouldHaveSingleItem().Text.ShouldBe(NoContextAnswer.Portuguese);
        events.OfType<AnswerCitationsEvent>().Single().Grounded.ShouldBeFalse();
        events.OfType<AnswerDoneEvent>().Single().Provider.ShouldBe(AnswerService.NoContextProvider);
        _chat.Requests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("", "conversation.message.required")]
    [InlineData(null, "conversation.notFound")]
    public async Task StreamAsync_InvalidRequest_SendsOnlyAnError(string? question, string expected)
    {
        var conversationId = question is null ? Guid.CreateVersion7() : _conversation.Id;

        var events = await CollectAsync(question ?? "Qual o raio?", conversationId);

        events.ShouldHaveSingleItem().ShouldBeOfType<AnswerErrorEvent>().Notifications.Single().Key.ShouldBe(expected);
        _recorded.ShouldBeEmpty();
    }

    [Fact]
    public async Task StreamAsync_ClientGoneMidAnswer_RecordsNothing()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in _service.StreamAsync(new AskQuestionCommand(_conversation.Id, "Qual o raio?"), cancellation.Token))
            {
                if (item is AnswerDeltaEvent)
                {
                    await cancellation.CancelAsync();
                }
            }
        });

        _recorded.ShouldBeEmpty();
    }

    [Fact]
    public async Task StreamAsync_RecordingFails_EndsWithTheErrorInsteadOfDone()
    {
        _bus.InvokeAsync<Result<RecordStreamedAnswerResponse>>(Arg.Any<object>(), Arg.Any<CancellationToken>(), Arg.Any<TimeSpan?>())
            .Returns(Result.Failure<RecordStreamedAnswerResponse>(ConversationNotifications.NotFound));

        var events = await CollectAsync("Qual o raio?");

        events[^1].ShouldBeOfType<AnswerErrorEvent>().Notifications.ShouldBe([ConversationNotifications.NotFound]);
        events.OfType<AnswerDoneEvent>().ShouldBeEmpty();
    }

    private async Task<List<AnswerStreamEvent>> CollectAsync(string question, Guid? conversationId = null)
    {
        List<AnswerStreamEvent> events = [];

        await foreach (var item in _service.StreamAsync(new AskQuestionCommand(conversationId ?? _conversation.Id, question), CancellationToken))
        {
            events.Add(item);
        }

        return events;
    }
}
