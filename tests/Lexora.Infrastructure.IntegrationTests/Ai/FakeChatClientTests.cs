using Lexora.Application.Abstractions.Ai;
using Lexora.Infrastructure.AI.Adapters;
using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers.Fake;
using Lexora.Infrastructure.AI.Registry;
using Microsoft.Extensions.AI;
using ChatTurn = Lexora.Application.Abstractions.Ai.ChatTurn;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class FakeChatClientTests
{
    private const string Answer = "This is a simulated answer to \"What is the vacation policy?\" based on the excerpts [1] and [2].";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetResponseAsync_Question_AnswersCitingTwoExcerptsAndCountsWords()
    {
        using var client = new FakeChatClient("fake-chat");

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.System, "Answer from the excerpts."), new ChatMessage(ChatRole.User, " What is the vacation policy? ")],
            cancellationToken: CancellationToken);

        response.Text.ShouldBe(Answer);
        response.ModelId.ShouldBe("fake-chat");
        (response.Usage!.InputTokenCount, response.Usage.OutputTokenCount).ShouldBe((9, 18));
    }

    [Fact]
    public async Task StreamAsync_ThroughTheAdapter_RebuildsTheSameAnswerWordByWord()
    {
        var chat = new AiChatSettings { DefaultProfile = "fake", Profiles = { ["fake"] = new AiProfileSettings { Provider = AiProviderKind.Fake, Model = "fake-chat" } } };
        using var registry = new ChatClientRegistry([new FakeChatClientProvider()], Microsoft.Extensions.Options.Options.Create(new AiOptions { Chat = chat }));
        var adapter = new ChatModelAdapter(registry, TimeProvider.System);
        var request = new ChatRequest(null, "Answer from the excerpts.", [new ChatTurn(ChatTurnRole.User, "What is the vacation policy?")]);

        List<ChatStreamUpdate> updates = [];
        await foreach (var update in adapter.StreamAsync(request, CancellationToken))
        {
            updates.Add(update);
        }

        updates.Count(update => update.TextDelta is not null).ShouldBe(18);
        string.Concat(updates.Select(update => update.TextDelta)).ShouldBe(Answer);
        var completed = updates[^1].Completed.ShouldNotBeNull();
        (completed.Text, completed.Provider, completed.Model, completed.InputTokens, completed.OutputTokens).ShouldBe((Answer, "fake", "fake-chat", 9, 18));
        (await adapter.CompleteAsync(request, CancellationToken)).Text.ShouldBe(Answer);
    }
}
