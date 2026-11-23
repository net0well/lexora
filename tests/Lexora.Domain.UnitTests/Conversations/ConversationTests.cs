using Lexora.Domain.Conversations;
using Lexora.Shared.Results;
using Lexora.TestKit;
using Microsoft.Extensions.Time.Testing;

namespace Lexora.Domain.UnitTests.Conversations;

public sealed class ConversationTests
{
    private const string Provider = "local";
    private const string Model = "qwen2.5:7b";

    private static readonly Guid _knowledgeBaseId = Guid.CreateVersion7();
    private static readonly Guid _ownerId = Guid.CreateVersion7();

    private readonly FakeTimeProvider _time = new(TestClock.Now);

    [Fact]
    public void Start_WithoutTitle_StartsEmptyAndUntitled()
    {
        var conversation = Conversation.Start(_knowledgeBaseId, _ownerId, null, TestClock.Now).Value;

        conversation.KnowledgeBaseId.ShouldBe(_knowledgeBaseId);
        conversation.UserId.ShouldBe(_ownerId);
        conversation.Title.ShouldBeEmpty();
        conversation.Messages.ShouldBeEmpty();
        conversation.CreatedAt.ShouldBe(TestClock.Now);
        conversation.UpdatedAt.ShouldBe(TestClock.Now);
    }

    [Fact]
    public void Start_WithTitle_KeepsTrimmedTitle() =>
        Conversation.Start(_knowledgeBaseId, _ownerId, "  Payment terms ", TestClock.Now).Value.Title.ShouldBe("Payment terms");

    [Fact]
    public void Start_TitleOverLimit_FailsAsTooLong() =>
        Conversation.Start(_knowledgeBaseId, _ownerId, new string('t', Conversation.MaxTitleLength + 1), TestClock.Now)
            .Notifications.Single().Key.ShouldBe("conversation.title.tooLong");

    [Fact]
    public void Start_EmptyOwner_Throws() =>
        Should.Throw<ArgumentException>(() => Conversation.Start(_knowledgeBaseId, Guid.Empty, null, TestClock.Now));

    [Fact]
    public void AddUserMessage_ValidQuestion_AppendsUserMessage()
    {
        var conversation = Start();
        _time.Advance(TimeSpan.FromMinutes(1));

        var result = conversation.AddUserMessage("  What are the payment terms?  ", Now);

        result.IsSuccess.ShouldBeTrue();
        var message = result.Value;
        message.Role.ShouldBe(MessageRole.User);
        message.Content.ShouldBe("What are the payment terms?");
        message.Citations.ShouldBeEmpty();
        message.Usage.ShouldBe(TokenUsage.None);
        message.Provider.ShouldBeNull();
        message.Model.ShouldBeNull();
        message.LatencyMs.ShouldBeNull();
        message.CreatedAt.ShouldBe(Now);
        conversation.Messages.ShouldBe([message]);
        conversation.UpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public void AddUserMessage_FirstQuestionWithoutTitle_UsesQuestionAsTitle()
    {
        var conversation = Start();

        conversation.AddUserMessage("What are\n the   payment terms?", Now);

        conversation.Title.ShouldBe("What are the payment terms?");
    }

    [Fact]
    public void AddUserMessage_LongFirstQuestion_TruncatesGeneratedTitle()
    {
        var conversation = Start();

        conversation.AddUserMessage(string.Join(' ', Enumerable.Repeat("clause", 40)), Now);

        conversation.Title.Length.ShouldBeLessThanOrEqualTo(Conversation.MaxTitleLength);
        conversation.Title.ShouldEndWith("...");
        conversation.Title.ShouldStartWith("clause clause");
    }

    [Fact]
    public void AddUserMessage_ConversationWithTitle_KeepsTitle()
    {
        var conversation = Conversation.Start(_knowledgeBaseId, _ownerId, "Contracts review", Now).Value;

        conversation.AddUserMessage("What are the payment terms?", Now);

        conversation.Title.ShouldBe("Contracts review");
    }

    [Fact]
    public void AddUserMessage_LaterQuestion_KeepsGeneratedTitle()
    {
        var conversation = Start();
        conversation.AddUserMessage("First question", Now);

        conversation.AddUserMessage("Second question", Now);

        conversation.Title.ShouldBe("First question");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddUserMessage_BlankQuestion_FailsAsRequired(string? question) =>
        ShouldReject(Start(), conversation => conversation.AddUserMessage(question, Now), "conversation.message.required", NotificationType.Validation);

    [Fact]
    public void AddUserMessage_QuestionOverLimit_FailsAsTooLong() =>
        ShouldReject(
            Start(),
            conversation => conversation.AddUserMessage(new string('q', Conversation.MaxQuestionLength + 1), Now),
            "conversation.message.tooLong",
            NotificationType.Validation);

    [Fact]
    public void AddUserMessage_QuestionAtLimit_Succeeds() =>
        Start().AddUserMessage(new string('q', Conversation.MaxQuestionLength), Now).IsSuccess.ShouldBeTrue();

    [Fact]
    public void AddUserMessage_ConversationAtMessageLimit_FailsAsBusinessRule() =>
        ShouldReject(Full(), conversation => conversation.AddUserMessage("One more?", Now), "conversation.limit.messages", NotificationType.BusinessRule);

    [Fact]
    public void AddMessages_SameOrEarlierTime_StayInTheOrderTheyWereAdded()
    {
        var conversation = Conversation.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), null, Now).Value;

        var question = conversation.AddUserMessage("What are the payment terms?", Now).Value;
        var answer = conversation.AddAssistantMessage("Due in 30 days [1].", [], TokenUsage.Create(10, 5), Provider, Model, 900, Now).Value;
        var followUp = conversation.AddUserMessage("And the penalty?", Now.AddSeconds(-5)).Value;

        question.CreatedAt.ShouldBe(Now);
        answer.CreatedAt.ShouldBe(Now.AddMicroseconds(1));
        followUp.CreatedAt.ShouldBe(Now.AddMicroseconds(2));
        conversation.UpdatedAt.ShouldBe(followUp.CreatedAt);
    }

    [Fact]
    public void AddAssistantMessage_ValidAnswer_AppendsWithCitationsAndUsage()
    {
        var conversation = Start();
        conversation.AddUserMessage("What are the payment terms?", Now);
        var citation = NewCitation(1);
        var usage = TokenUsage.Create(1200, 85);
        _time.Advance(TimeSpan.FromSeconds(3));

        var result = conversation.AddAssistantMessage("Payment is due in 30 days [1].", [citation], usage, Provider, Model, 2900, Now);

        result.IsSuccess.ShouldBeTrue();
        var message = result.Value;
        message.Role.ShouldBe(MessageRole.Assistant);
        message.Content.ShouldBe("Payment is due in 30 days [1].");
        message.Citations.ShouldBe([citation]);
        message.Usage.ShouldBe(usage);
        message.Provider.ShouldBe(Provider);
        message.Model.ShouldBe(Model);
        message.LatencyMs.ShouldBe(2900);
        conversation.Messages.Count.ShouldBe(2);
        conversation.UpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public void AddAssistantMessage_ConversationAtMessageLimit_FailsAsBusinessRule() =>
        ShouldReject(Full(), conversation => AddAnswer(conversation, "Late answer."), "conversation.limit.messages", NotificationType.BusinessRule);

    [Fact]
    public void AddAssistantMessage_BlankAnswer_Throws() => Should.Throw<ArgumentException>(() => AddAnswer(Start(), " "));

    [Fact]
    public void AddAssistantMessage_NegativeLatency_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Start().AddAssistantMessage("Answer.", [], TokenUsage.None, Provider, Model, -1, Now));

    [Fact]
    public void Rename_ValidTitle_ChangesTitle()
    {
        var conversation = Start();
        _time.Advance(TimeSpan.FromMinutes(1));

        conversation.Rename(" Supplier terms ", Now).IsSuccess.ShouldBeTrue();

        conversation.Title.ShouldBe("Supplier terms");
        conversation.UpdatedAt.ShouldBe(Now);
    }

    [Theory]
    [InlineData(null, "conversation.title.required")]
    [InlineData("  ", "conversation.title.required")]
    public void Rename_BlankTitle_FailsAsRequired(string? title, string expectedKey)
    {
        var conversation = Conversation.Start(_knowledgeBaseId, _ownerId, "Original", Now).Value;

        conversation.Rename(title, Now).Notifications.Single().Key.ShouldBe(expectedKey);
        conversation.Title.ShouldBe("Original");
    }

    [Fact]
    public void Rename_TitleOverLimit_FailsAsTooLong() =>
        Start().Rename(new string('t', Conversation.MaxTitleLength + 1), Now).Notifications.Single().Key.ShouldBe("conversation.title.tooLong");

    [Fact]
    public void IsOwnedBy_Owner_ReturnsTrue() => Start().IsOwnedBy(_ownerId).ShouldBeTrue();

    [Fact]
    public void IsOwnedBy_AnotherUser_ReturnsFalse() => Start().IsOwnedBy(Guid.CreateVersion7()).ShouldBeFalse();

    [Fact]
    public void RecentHistory_MoreMessagesThanRequested_ReturnsLatestInChronologicalOrder()
    {
        var conversation = Start();
        conversation.AddUserMessage("Q1", Now);
        AddAnswer(conversation, "A1");
        conversation.AddUserMessage("Q2", Now);
        AddAnswer(conversation, "A2");

        conversation.RecentHistory(3).Select(message => message.Content).ShouldBe(["A1", "Q2", "A2"]);
    }

    [Fact]
    public void RecentHistory_FewerMessagesThanRequested_ReturnsAll()
    {
        var conversation = Start();
        conversation.AddUserMessage("Q1", Now);

        conversation.RecentHistory(6).Select(message => message.Content).ShouldBe(["Q1"]);
    }

    [Fact]
    public void RecentHistory_Zero_ReturnsEmpty()
    {
        var conversation = Start();
        conversation.AddUserMessage("Q1", Now);

        conversation.RecentHistory(0).ShouldBeEmpty();
    }

    [Fact]
    public void RecentHistory_Negative_Throws() => Should.Throw<ArgumentOutOfRangeException>(() => Start().RecentHistory(-1));

    private DateTimeOffset Now => _time.GetUtcNow();

    private Conversation Start() => Conversation.Start(_knowledgeBaseId, _ownerId, null, Now).Value;

    private Conversation Full()
    {
        var conversation = Start();

        for (var exchange = 0; exchange < Conversation.MaxMessages / 2; exchange++)
        {
            conversation.AddUserMessage($"Question {exchange}", Now);
            AddAnswer(conversation, $"Answer {exchange}");
        }

        conversation.Messages.Count.ShouldBe(Conversation.MaxMessages);

        return conversation;
    }

    private Result<ConversationMessage> AddAnswer(Conversation conversation, string answer) =>
        conversation.AddAssistantMessage(answer, [], TokenUsage.Create(10, 5), Provider, Model, 100, Now);

    private void ShouldReject(
        Conversation conversation,
        Func<Conversation, Result<ConversationMessage>> addMessage,
        string expectedKey,
        NotificationType expectedType)
    {
        var countBefore = conversation.Messages.Count;
        var updatedBefore = conversation.UpdatedAt;
        _time.Advance(TimeSpan.FromMinutes(1));

        var result = addMessage(conversation);

        result.Notifications.Single().ShouldSatisfyAllConditions(
            notification => notification.Key.ShouldBe(expectedKey),
            notification => notification.Type.ShouldBe(expectedType));
        conversation.Messages.Count.ShouldBe(countBefore);
        conversation.UpdatedAt.ShouldBe(updatedBefore);
    }

    private static Citation NewCitation(int index) =>
        Citation.Create(index, Guid.CreateVersion7(), Guid.CreateVersion7(), "contract.pdf", 4, null, "Payment is due within 30 days.", 0.031);
}
