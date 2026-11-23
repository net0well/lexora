using Lexora.Application.Conversations.AskQuestion;
using Lexora.Application.Conversations.RecordStreamedAnswer;
using Lexora.Domain.Conversations;
using Lexora.TestKit;

namespace Lexora.Application.UnitTests.Conversations;

public sealed class ConversationCommandPrintingTests
{
    private const string Question = "Qual é o salário do diretor?";
    private const string Answer = "O salário do diretor é confidencial [1].";

    [Fact]
    public void ToString_AskQuestion_KeepsTheQuestionOut() =>
        new AskQuestionCommand(Guid.CreateVersion7(), Question, "local").ToString().ShouldSatisfyAllConditions(
            printed => printed.ShouldNotContain("salário"),
            printed => printed.ShouldContain($"Question = <{Question.Length} chars>"),
            printed => printed.ShouldContain("Profile = local"));

    [Fact]
    public void ToString_RecordStreamedAnswer_KeepsTheConversationOut()
    {
        var command = new RecordStreamedAnswerCommand(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Question, TestClock.Now, Answer, [], TokenUsage.Create(10, 5), "ollama", "qwen2.5:7b", 900);

        command.ToString().ShouldSatisfyAllConditions(
            printed => printed.ShouldNotContain("salário"),
            printed => printed.ShouldContain($"Answer = <{Answer.Length} chars>"),
            printed => printed.ShouldContain("Model = qwen2.5:7b"));
    }
}
