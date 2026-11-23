using System.Net;
using Anthropic.Exceptions;
using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers.Anthropic;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class AnthropicProviderTests : IAsyncLifetime
{
    private StubAnthropicServer _server = null!;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => _server = await StubAnthropicServer.StartAsync(CancellationToken);

    public async ValueTask DisposeAsync() => await _server.DisposeAsync();

    [Fact]
    public async Task Chat_Profile_SendsTheSystemPromptSeparatelyWithTheKeyAndNoSampling()
    {
        using var client = new AnthropicChatClientProvider().Create("claude", Profile());

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.System, "Answer from the excerpts."), new ChatMessage(ChatRole.User, "And for interns?")],
            new ChatOptions { Temperature = 0.1f, MaxOutputTokens = 2000 },
            CancellationToken);

        response.Text.ShouldBe("Interns get 15 days [2].");
        (response.Usage!.InputTokenCount, response.Usage.OutputTokenCount).ShouldBe((12, 5));
        var request = _server.Requests.ShouldHaveSingleItem();
        request.Authorization.ShouldBe("Bearer sk-ant-test");
        var body = StubOpenAiServer.BodyOf(request);
        body.GetProperty("model").GetString().ShouldBe("claude-sonnet-5-5");
        body.GetProperty("max_tokens").GetInt32().ShouldBe(2000);
        body.GetProperty("system").ToString().ShouldContain("Answer from the excerpts.");
        body.GetProperty("messages").EnumerateArray().Select(message => message.GetProperty("role").GetString()).ShouldBe(["user"]);
        body.TryGetProperty("temperature", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Chat_TooManyRequests_IsNotRetriedInsideTheSdk()
    {
        _server.FailNext(HttpStatusCode.TooManyRequests, "rate_limit_error");
        using var client = new AnthropicChatClientProvider().Create("claude", Profile());

        await Should.ThrowAsync<AnthropicRateLimitException>(() => client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")], cancellationToken: CancellationToken));

        _server.Requests.Count.ShouldBe(1);
    }

    private AiProfileSettings Profile() =>
        new() { Provider = AiProviderKind.Anthropic, Model = "claude-sonnet-5-5", ApiKey = "sk-ant-test", Endpoint = _server.BaseAddress, MaxOutputTokens = 16000 };
}
