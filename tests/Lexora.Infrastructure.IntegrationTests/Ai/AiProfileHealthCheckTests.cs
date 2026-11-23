using Lexora.Infrastructure.AI.Health;
using Lexora.Infrastructure.AI.Options;
using Lexora.Infrastructure.AI.Providers.Fake;
using Lexora.Infrastructure.AI.Providers.Ollama;
using Lexora.Infrastructure.AI.Registry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class AiProfileHealthCheckTests : IAsyncLifetime
{
    private WebApplication _ollama = null!;
    private Uri _endpoint = null!;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _ollama = builder.Build();
        _ollama.MapGet("/api/tags", () => Results.Json(new { models = new[] { new { name = "qwen2.5:7b" }, new { name = "bge-m3:latest" } } }));
        await _ollama.StartAsync(CancellationToken);
        _endpoint = new Uri(_ollama.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First() + "/");
    }

    public async ValueTask DisposeAsync() => await _ollama.DisposeAsync();

    [Theory]
    [InlineData("qwen2.5:7b", HealthStatus.Healthy)]
    [InlineData("llama3.2:3b", HealthStatus.Degraded)]
    public async Task Chat_OllamaProfile_IsHealthyOnlyWhenItsModelIsPulled(string model, HealthStatus expected)
    {
        var result = await ChatCheck(Ollama(model, _endpoint)).CheckHealthAsync(Context(), CancellationToken);

        result.Status.ShouldBe(expected);
    }

    [Fact]
    public async Task Chat_OllamaNotRunning_IsDegraded()
    {
        var result = await ChatCheck(Ollama("qwen2.5:7b", new Uri("http://127.0.0.1:9/"))).CheckHealthAsync(Context(), CancellationToken);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldBe("Ollama is not answering for profile 'active'.");
    }

    [Fact]
    public async Task Chat_ProfileWithoutNetworkProbe_IsHealthyWhenConfigured() =>
        (await ChatCheck(new AiProfileSettings { Provider = AiProviderKind.Fake, Model = "fake-chat" }).CheckHealthAsync(Context(), CancellationToken))
            .Status.ShouldBe(HealthStatus.Healthy);

    [Theory]
    [InlineData("bge-m3", HealthStatus.Healthy)]
    [InlineData("nomic-embed-text", HealthStatus.Degraded)]
    public async Task Embedding_OllamaProfile_AcceptsTheLatestTagAndNeedsThePulledModel(string model, HealthStatus expected)
    {
        var options = new AiOptions
        {
            Embedding = new AiEmbeddingSettings { Profile = "active", Profiles = { ["active"] = Ollama(model, _endpoint) } },
        };
        using var registry = new EmbeddingGeneratorRegistry(
            [new OllamaEmbeddingGeneratorProvider()],
            Microsoft.Extensions.Options.Options.Create(options),
            NullLoggerFactory.Instance);

        (await new EmbeddingProfileHealthCheck(registry).CheckHealthAsync(Context(), CancellationToken)).Status.ShouldBe(expected);
    }

    private static AiProfileSettings Ollama(string model, Uri endpoint) => new() { Provider = AiProviderKind.Ollama, Model = model, Endpoint = endpoint };

    private static ChatProfileHealthCheck ChatCheck(AiProfileSettings profile)
    {
        var options = new AiOptions { Chat = new AiChatSettings { DefaultProfile = "active", Profiles = { ["active"] = profile } } };
        var registry = new ChatClientRegistry(
            [new OllamaChatClientProvider(), new FakeChatClientProvider()],
            Microsoft.Extensions.Options.Options.Create(options),
            NullLoggerFactory.Instance);

        return new ChatProfileHealthCheck(registry);
    }

    private static HealthCheckContext Context() =>
        new() { Registration = new HealthCheckRegistration("ai", _ => throw new NotSupportedException(), HealthStatus.Degraded, tags: null) };
}
