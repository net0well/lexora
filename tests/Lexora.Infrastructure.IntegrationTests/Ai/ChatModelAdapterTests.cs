using Lexora.Application.Abstractions.Ai;
using Lexora.Infrastructure.AI.Adapters;
using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Registry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Time.Testing;
using ChatTurn = Lexora.Application.Abstractions.Ai.ChatTurn;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class ChatModelAdapterTests
{
    private readonly FakeTimeProvider _time = new();
    private readonly StubChatClientProvider _ollama = new(AiProviderKind.Ollama);
    private readonly ChatModelAdapter _adapter;

    public ChatModelAdapterTests()
    {
        var chat = new AiChatSettings
        {
            DefaultProfile = "local",
            Profiles =
            {
                ["local"] = new AiProfileSettings { Provider = AiProviderKind.Ollama, Model = "qwen2.5:7b", Endpoint = new Uri("http://localhost:11434") },
                ["precise"] = new AiProfileSettings
                {
                    Provider = AiProviderKind.Ollama,
                    Model = "qwen2.5:14b",
                    Endpoint = new Uri("http://localhost:11434"),
                    Temperature = 0,
                    MaxOutputTokens = 300,
                },
            },
        };

        _adapter = new ChatModelAdapter(new ChatClientRegistry([_ollama], Microsoft.Extensions.Options.Options.Create(new AiOptions { Chat = chat })), _time);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private StubChatClient Client(string profile = "local") => _ollama.ClientFor(profile);

    [Fact]
    public async Task CompleteAsync_Request_SendsTheSystemPromptAndTurnsInOrderWithTheProfileDefaults()
    {
        await _adapter.CompleteAsync(Request(), CancellationToken);

        var (messages, options) = Client().Calls.ShouldHaveSingleItem();
        messages.Select(message => (message.Role, message.Text)).ShouldBe(
        [
            (ChatRole.System, "Answer from the excerpts."),
            (ChatRole.User, "What is the vacation policy?"),
            (ChatRole.Assistant, "30 days [1]."),
            (ChatRole.User, "And for interns?"),
        ]);
        (options!.ModelId, options.Temperature, options.MaxOutputTokens).ShouldBe(("qwen2.5:7b", 0.1f, 1024));
    }

    [Fact]
    public async Task CompleteAsync_ProfileAndRequestSettings_RequestWinsOverProfile()
    {
        await _adapter.CompleteAsync(Request("precise") with { MaxOutputTokens = 50 }, CancellationToken);

        var options = Client("precise").Calls.ShouldHaveSingleItem().Options!;
        (options.ModelId, options.Temperature, options.MaxOutputTokens).ShouldBe(("qwen2.5:14b", 0f, 50));
    }

    [Fact]
    public async Task CompleteAsync_ProviderAnswer_BecomesACompletionWithUsageAndLatency()
    {
        Client().Response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Interns get 15 days [2]."))
        {
            ModelId = "qwen2.5:7b-instruct",
            Usage = new UsageDetails { InputTokenCount = 120, OutputTokenCount = 8 },
        };
        Client().OnCall = () => _time.Advance(TimeSpan.FromMilliseconds(1500));

        var completion = await _adapter.CompleteAsync(Request(), CancellationToken);

        completion.ShouldBe(new ChatCompletion("Interns get 15 days [2].", "ollama", "qwen2.5:7b-instruct", 120, 8, TimeSpan.FromMilliseconds(1500)));
    }

    [Fact]
    public async Task StreamAsync_ProviderUpdates_AreRelayedAndFinishWithTheWholeAnswer()
    {
        Client().Updates =
        [
            new ChatResponseUpdate(ChatRole.Assistant, "Interns get ") { ModelId = "qwen2.5:7b" },
            new ChatResponseUpdate(ChatRole.Assistant, "15 days [2]."),
            new ChatResponseUpdate(ChatRole.Assistant, [new UsageContent(new UsageDetails { InputTokenCount = 120, OutputTokenCount = 8 })]),
        ];

        List<ChatStreamUpdate> updates = [];
        await foreach (var update in _adapter.StreamAsync(Request(), CancellationToken))
        {
            updates.Add(update);
        }

        updates.Select(update => update.TextDelta).ShouldBe(["Interns get ", "15 days [2].", null]);
        updates[^1].Completed.ShouldBe(new ChatCompletion("Interns get 15 days [2].", "ollama", "qwen2.5:7b", 120, 8, TimeSpan.Zero));
    }

    [Fact]
    public async Task CompleteAsync_UnknownProfile_Throws() =>
        await Should.ThrowAsync<InvalidOperationException>(() => _adapter.CompleteAsync(Request("gemini"), CancellationToken));

    private static ChatRequest Request(string? profile = null) =>
        new(profile, "Answer from the excerpts.",
        [
            new ChatTurn(ChatTurnRole.User, "What is the vacation policy?"),
            new ChatTurn(ChatTurnRole.Assistant, "30 days [1]."),
            new ChatTurn(ChatTurnRole.User, "And for interns?"),
        ]);
}
