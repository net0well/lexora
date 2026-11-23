using System.ClientModel;
using System.Net;
using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers.OpenAi;
using Microsoft.Extensions.AI;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class OpenAiProvidersTests : IAsyncLifetime
{
    private StubOpenAiServer _server = null!;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => _server = await StubOpenAiServer.StartAsync(CancellationToken);

    public async ValueTask DisposeAsync() => await _server.DisposeAsync();

    [Fact]
    public async Task Chat_Profile_SendsTheModelMessagesAndKeyAndReadsTheAnswer()
    {
        using var client = new OpenAiChatClientProvider().Create("openai", Profile("gpt-5-mini"));

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.System, "Answer from the excerpts."), new ChatMessage(ChatRole.User, "And for interns?")],
            new ChatOptions { MaxOutputTokens = 300 },
            CancellationToken);

        response.Text.ShouldBe("Interns get 15 days [2].");
        (response.Usage!.InputTokenCount, response.Usage.OutputTokenCount).ShouldBe((12, 5));
        var request = _server.Requests.ShouldHaveSingleItem();
        request.Path.ShouldBe("/v1/chat/completions");
        request.Authorization.ShouldBe("Bearer test-key");
        var body = StubOpenAiServer.BodyOf(request);
        body.GetProperty("model").GetString().ShouldBe("gpt-5-mini");
        body.GetProperty("messages").EnumerateArray().Select(message => message.GetProperty("role").GetString()).ShouldBe(["system", "user"]);
    }

    [Fact]
    public async Task Embedding_Profile_AsksForTheConfiguredDimensions()
    {
        var provider = new OpenAiEmbeddingGeneratorProvider();
        using var generator = provider.Create("openai", Profile("text-embedding-3-small"));

        var embeddings = await generator.GenerateAsync(["first", "second"], new EmbeddingGenerationOptions { Dimensions = 4 }, CancellationToken);

        provider.AcceptsDimensions.ShouldBeTrue();
        embeddings.Select(embedding => embedding.Vector.Length).ShouldBe([4, 4]);
        embeddings[1].Vector.ToArray()[0].ShouldBe(2f);
        StubOpenAiServer.BodyOf(_server.Requests.ShouldHaveSingleItem()).GetProperty("dimensions").GetInt32().ShouldBe(4);
    }

    [Fact]
    public async Task Chat_TooManyRequests_IsNotRetriedInsideTheSdk()
    {
        _server.FailNext(HttpStatusCode.TooManyRequests, retryAfter: "1", times: 3);
        using var client = new OpenAiChatClientProvider().Create("openai", Profile("gpt-5-mini"));

        var failure = await Should.ThrowAsync<ClientResultException>(() => client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")], cancellationToken: CancellationToken));

        failure.Status.ShouldBe(429);
        _server.Requests.Count.ShouldBe(1);
    }

    private AiProfileSettings Profile(string model) =>
        new() { Provider = AiProviderKind.OpenAI, Model = model, ApiKey = "test-key", Endpoint = _server.BaseAddress };
}
