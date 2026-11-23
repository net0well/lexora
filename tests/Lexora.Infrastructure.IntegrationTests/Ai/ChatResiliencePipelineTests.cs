using System.ClientModel;
using System.Net;
using Lexora.Application.Abstractions.Ai;
using Lexora.Infrastructure.AI.Adapters;
using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers.OpenAiCompatible;
using Lexora.Infrastructure.AI.Registry;
using Lexora.Infrastructure.AI.Resilience;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Polly.CircuitBreaker;
using ChatTurn = Lexora.Application.Abstractions.Ai.ChatTurn;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class ChatResiliencePipelineTests : IAsyncLifetime
{
    private static readonly ChatRequest _request = new(null, "Answer from the excerpts.", [new ChatTurn(ChatTurnRole.User, "And for interns?")]);

    private StubOpenAiServer _server = null!;
    private ChatClientRegistry _registry = null!;
    private ChatModelAdapter _adapter = null!;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _server = await StubOpenAiServer.StartAsync(CancellationToken);
        var chat = new AiChatSettings
        {
            DefaultProfile = "groq",
            Profiles =
            {
                ["groq"] = new AiProfileSettings
                {
                    Provider = AiProviderKind.OpenAICompatible,
                    Model = "openai/gpt-oss-120b",
                    Endpoint = _server.BaseAddress,
                    ApiKey = "gsk-test",
                },
            },
        };
        _registry = new ChatClientRegistry([new OpenAiCompatibleChatClientProvider()], Microsoft.Extensions.Options.Options.Create(new AiOptions { Chat = chat }), NullLoggerFactory.Instance);
        _adapter = new ChatModelAdapter(_registry, new AiResiliencePipelineProvider(TimeProvider.System), TimeProvider.System);
    }

    public async ValueTask DisposeAsync()
    {
        _registry.Dispose();
        await _server.DisposeAsync();
    }

    [Fact]
    public async Task CompleteAsync_OneTooManyRequests_IsRetriedOnceAndSucceeds()
    {
        _server.FailNext(HttpStatusCode.TooManyRequests, retryAfter: "0");

        var completion = await _adapter.CompleteAsync(_request, CancellationToken);

        completion.Text.ShouldBe("Interns get 15 days [2].");
        _server.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task CompleteAsync_ProviderStillThrottling_SurfacesAsUnavailableWithItsRetryAfter()
    {
        _server.FailNext(HttpStatusCode.TooManyRequests, retryAfter: "0", times: 2);

        var exception = await Should.ThrowAsync<AiProviderUnavailableException>(() => _adapter.CompleteAsync(_request, CancellationToken));

        (exception.Provider, exception.Profile, exception.RetryAfter).ShouldBe(("openaicompatible", "groq", TimeSpan.Zero));
        exception.InnerException.ShouldBeOfType<ClientResultException>().Status.ShouldBe(429);
        _server.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task CompleteAsync_LongRetryAfter_IsLeftToTheMessageRetries()
    {
        _server.FailNext(HttpStatusCode.TooManyRequests, retryAfter: "60");

        var exception = await Should.ThrowAsync<AiProviderUnavailableException>(() => _adapter.CompleteAsync(_request, CancellationToken));

        exception.RetryAfter.ShouldBe(TimeSpan.FromMinutes(1));
        _server.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CompleteAsync_InvalidKey_IsNotTreatedAsTransient()
    {
        _server.FailNext(HttpStatusCode.Unauthorized);

        (await Should.ThrowAsync<ClientResultException>(() => _adapter.CompleteAsync(_request, CancellationToken))).Status.ShouldBe(401);
        _server.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CompleteAsync_ProviderDownForSeveralCalls_OpensTheCircuitWithoutCallingIt()
    {
        _server.FailNext(HttpStatusCode.ServiceUnavailable, retryAfter: "0", times: AiResiliencePipelineProvider.MinimumThroughput);

        // Two calls with a retry each, then the third call's first failure opens the circuit before its retry.
        for (var call = 0; call < 3; call++)
        {
            await Should.ThrowAsync<AiProviderUnavailableException>(() => _adapter.CompleteAsync(_request, CancellationToken));
        }

        var exception = await Should.ThrowAsync<AiProviderUnavailableException>(() => _adapter.CompleteAsync(_request, CancellationToken));

        exception.InnerException.ShouldBeOfType<BrokenCircuitException>();
        _server.Requests.Count.ShouldBe(AiResiliencePipelineProvider.MinimumThroughput);
    }

    [Fact]
    public async Task StreamAsync_FailureBeforeTheFirstUpdate_IsRetried()
    {
        var (adapter, client) = StubAdapter();
        client.Failures.Enqueue(new HttpRequestException("busy", null, HttpStatusCode.ServiceUnavailable));

        var updates = await CollectAsync(adapter);

        updates[^1].Completed.ShouldNotBeNull().Text.ShouldBe("Interns get 15 days [2].");
        client.Calls.Count.ShouldBe(2);
    }

    [Fact]
    public async Task StreamAsync_FailureAfterTextWasSent_IsNotRetried()
    {
        var (adapter, client) = StubAdapter();
        client.FailAfterFirstUpdate = new HttpRequestException("connection reset");
        List<string?> received = [];

        await Should.ThrowAsync<AiProviderUnavailableException>(async () =>
        {
            await foreach (var update in adapter.StreamAsync(_request, CancellationToken))
            {
                received.Add(update.TextDelta);
            }
        });

        received.ShouldBe(["Interns get "]);
        client.Calls.Count.ShouldBe(1);
    }

    private static (ChatModelAdapter Adapter, StubChatClient Client) StubAdapter()
    {
        var provider = new StubChatClientProvider(AiProviderKind.Fake);
        var client = provider.ClientFor("fake");
        client.Updates = [new ChatResponseUpdate(ChatRole.Assistant, "Interns get "), new ChatResponseUpdate(ChatRole.Assistant, "15 days [2].")];
        var chat = new AiChatSettings { DefaultProfile = "fake", Profiles = { ["fake"] = new AiProfileSettings { Provider = AiProviderKind.Fake, Model = "fake-chat" } } };
        var registry = new ChatClientRegistry([provider], Microsoft.Extensions.Options.Options.Create(new AiOptions { Chat = chat }), NullLoggerFactory.Instance);

        return (new ChatModelAdapter(registry, new AiResiliencePipelineProvider(TimeProvider.System), TimeProvider.System), client);
    }

    private static async Task<List<ChatStreamUpdate>> CollectAsync(ChatModelAdapter adapter)
    {
        List<ChatStreamUpdate> updates = [];

        await foreach (var update in adapter.StreamAsync(_request, CancellationToken))
        {
            updates.Add(update);
        }

        return updates;
    }
}
