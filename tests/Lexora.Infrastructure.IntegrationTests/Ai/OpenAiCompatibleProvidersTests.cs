using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers.OpenAiCompatible;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class OpenAiCompatibleProvidersTests : IAsyncLifetime
{
    private StubOpenAiServer _server = null!;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => _server = await StubOpenAiServer.StartAsync(CancellationToken);

    public async ValueTask DisposeAsync() => await _server.DisposeAsync();

    [Fact]
    public async Task Chat_ProfileWithKey_CallsTheConfiguredEndpointWithTheKey()
    {
        using var client = new OpenAiCompatibleChatClientProvider().Create("groq", Profile("openai/gpt-oss-120b", apiKey: "gsk-test"));

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "And for interns?")], cancellationToken: CancellationToken);

        response.Text.ShouldBe("Interns get 15 days [2].");
        var request = _server.Requests.ShouldHaveSingleItem();
        request.Authorization.ShouldBe("Bearer gsk-test");
        StubOpenAiServer.BodyOf(request).GetProperty("model").GetString().ShouldBe("openai/gpt-oss-120b");
    }

    [Fact]
    public async Task Chat_LocalServerWithoutKey_StillCalls()
    {
        using var client = new OpenAiCompatibleChatClientProvider().Create("lmstudio", Profile("local-model", apiKey: null));

        (await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")], cancellationToken: CancellationToken)).Text.ShouldNotBeEmpty();

        _server.Requests.ShouldHaveSingleItem().Path.ShouldBe("/v1/chat/completions");
    }

    [Fact]
    public async Task Embedding_Profile_DoesNotAskForDimensions()
    {
        var provider = new OpenAiCompatibleEmbeddingGeneratorProvider();
        using var generator = provider.Create("compatible", Profile("nomic-embed-text", apiKey: null));

        var embeddings = await generator.GenerateAsync(["first"], cancellationToken: CancellationToken);

        provider.AcceptsDimensions.ShouldBeFalse();
        embeddings.ShouldHaveSingleItem().Vector.Length.ShouldBe(3);
        StubOpenAiServer.BodyOf(_server.Requests.ShouldHaveSingleItem()).TryGetProperty("dimensions", out _).ShouldBeFalse();
    }

    private AiProfileSettings Profile(string model, string? apiKey) =>
        new() { Provider = AiProviderKind.OpenAICompatible, Model = model, ApiKey = apiKey, Endpoint = _server.BaseAddress };
}
