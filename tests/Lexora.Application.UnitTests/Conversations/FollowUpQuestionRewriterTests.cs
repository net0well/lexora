using Lexora.Application.Abstractions.Ai;
using Lexora.Application.Conversations.Prompts;
using Lexora.Domain.Conversations;
using Lexora.TestKit;
using Lexora.TestKit.Fakes;

namespace Lexora.Application.UnitTests.Conversations;

public sealed class FollowUpQuestionRewriterTests
{
    private readonly FakeChatModel _chat = new();
    private readonly FollowUpQuestionRewriter _rewriter;

    public FollowUpQuestionRewriterTests() => _rewriter = new FollowUpQuestionRewriter(_chat);

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RewriteAsync_FirstQuestion_KeepsItWithoutCallingTheModel()
    {
        (await _rewriter.RewriteAsync("Qual o raio de isolamento?", [], profile: null, CancellationToken)).ShouldBe("Qual o raio de isolamento?");

        _chat.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task RewriteAsync_FollowUp_SendsTheHistoryAndReturnsTheCleanedQuestion()
    {
        _chat.Answers.Enqueue("  “Quais luvas usar em caso de vazamento químico?”  ");

        var rewritten = await _rewriter.RewriteAsync("E quais luvas?", History(), profile: "claude", CancellationToken);

        rewritten.ShouldBe("Quais luvas usar em caso de vazamento químico?");
        var request = _chat.Requests.ShouldHaveSingleItem();
        (request.Profile, request.Temperature, request.MaxOutputTokens).ShouldBe(("claude", 0f, FollowUpQuestionRewriter.MaxOutputTokens));
        request.Turns.Select(turn => turn.Role).ShouldBe([ChatTurnRole.User, ChatTurnRole.Assistant, ChatTurnRole.User]);
        request.Turns[^1].Content.ShouldBe("Question to rewrite: E quais luvas?");
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("Let me think about the conversation first.\nQuais luvas usar?")]
    public async Task RewriteAsync_UnusableReply_FallsBackToTheOriginalQuestion(string reply)
    {
        _chat.Answers.Enqueue(reply);

        (await _rewriter.RewriteAsync("E quais luvas?", History(), profile: null, CancellationToken)).ShouldBe("E quais luvas?");
    }

    private static IReadOnlyList<ConversationMessage> History()
    {
        var conversation = Conversation.Start(Guid.CreateVersion7(), Guid.CreateVersion7(), null, TestClock.Now).Value;
        conversation.AddUserMessage("O que fazer em vazamento químico?", TestClock.Now);
        conversation.AddAssistantMessage("Isole a área [1].", [], TokenUsage.Create(10, 4), "ollama", "qwen2.5:7b", 900, TestClock.Now);

        return conversation.Messages;
    }
}
