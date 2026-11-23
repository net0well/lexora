using Lexora.Application.Abstractions.Ai;

namespace Lexora.Application.UnitTests.Abstractions;

public sealed class ChatContractPrintingTests
{
    private const string Secret = "salary of the director";

    public static TheoryData<object> Contracts => new()
    {
        new ChatTurn(ChatTurnRole.User, Secret),
        new ChatRequest("local", Secret, [new ChatTurn(ChatTurnRole.User, Secret)]),
        new ChatCompletion(Secret, "ollama", "qwen2.5:7b", 10, 20, TimeSpan.FromSeconds(1)),
        ChatStreamUpdate.Delta(Secret),
        ChatStreamUpdate.Finished(new ChatCompletion(Secret, "ollama", "qwen2.5:7b", 10, 20, TimeSpan.FromSeconds(1))),
    };

    [Theory]
    [MemberData(nameof(Contracts))]
    public void ToString_ChatContract_ShowsTheShapeButNotTheConversation(object contract)
    {
        var printed = contract.ToString().ShouldNotBeNull();

        printed.ShouldNotContain(Secret);
        printed.ShouldContain($"<{Secret.Length} chars>");
    }

    [Fact]
    public void ToString_Request_KeepsTheSettingsThatHelpDiagnose()
    {
        var request = new ChatRequest("claude", "system", [new ChatTurn(ChatTurnRole.User, "question")], Temperature: 0.1f, MaxOutputTokens: 512);

        request.ToString().ShouldBe("ChatRequest { Profile = claude, SystemPrompt = <6 chars>, Turns = 1, Temperature = 0.1, MaxOutputTokens = 512 }");
    }
}
