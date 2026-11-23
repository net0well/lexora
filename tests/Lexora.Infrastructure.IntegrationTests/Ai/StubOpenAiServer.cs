using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class StubOpenAiServer : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly ConcurrentQueue<(HttpStatusCode Status, string? RetryAfter)> _failures = new();

    private StubOpenAiServer(WebApplication app) => _app = app;

    public ConcurrentQueue<StubOpenAiRequest> Requests { get; } = new();

    public string ChatAnswer { get; set; } = "Interns get 15 days [2].";

    public Uri BaseAddress { get; private set; } = null!;

    public static async Task<StubOpenAiServer> StartAsync(CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var server = new StubOpenAiServer(builder.Build());
        server._app.MapPost("/v1/chat/completions", (Func<HttpContext, Task<IResult>>)server.ChatAsync);
        server._app.MapPost("/v1/embeddings", (Func<HttpContext, Task<IResult>>)server.EmbeddingsAsync);

        await server._app.StartAsync(cancellationToken);
        var address = server._app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        server.BaseAddress = new Uri($"{address}/v1/");

        return server;
    }

    public void FailNext(HttpStatusCode status, string? retryAfter = null, int times = 1)
    {
        for (var attempt = 0; attempt < times; attempt++)
        {
            _failures.Enqueue((status, retryAfter));
        }
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    private async Task<IResult> ChatAsync(HttpContext context)
    {
        var body = await RecordAsync(context);

        if (Failure(context) is { } failure)
        {
            return failure;
        }

        return Results.Json(new
        {
            id = "chatcmpl-1",
            @object = "chat.completion",
            created = 1,
            model = body["model"]!.GetValue<string>(),
            choices = new[] { new { index = 0, message = new { role = "assistant", content = ChatAnswer }, finish_reason = "stop" } },
            usage = new { prompt_tokens = 12, completion_tokens = 5, total_tokens = 17 },
        });
    }

    private async Task<IResult> EmbeddingsAsync(HttpContext context)
    {
        var body = await RecordAsync(context);

        if (Failure(context) is { } failure)
        {
            return failure;
        }

        var inputs = body["input"] is JsonArray array ? array.Count : 1;
        var dimensions = body["dimensions"]?.GetValue<int>() ?? 3;
        var base64 = body["encoding_format"]?.GetValue<string>() == "base64";

        return Results.Json(new
        {
            @object = "list",
            model = body["model"]!.GetValue<string>(),
            data = Enumerable.Range(0, inputs).Select(index => new
            {
                @object = "embedding",
                index,
                embedding = base64 ? (object)Convert.ToBase64String(Vector(index, dimensions).SelectMany(BitConverter.GetBytes).ToArray()) : Vector(index, dimensions),
            }),
            usage = new { prompt_tokens = inputs, total_tokens = inputs },
        });
    }

    private async Task<JsonObject> RecordAsync(HttpContext context)
    {
        var body = (await JsonNode.ParseAsync(context.Request.Body))!.AsObject();
        Requests.Enqueue(new StubOpenAiRequest(context.Request.Path, context.Request.Headers.Authorization.ToString(), body.ToJsonString()));

        return body;
    }

    private IResult? Failure(HttpContext context)
    {
        if (!_failures.TryDequeue(out var failure))
        {
            return null;
        }

        if (failure.RetryAfter is { } retryAfter)
        {
            context.Response.Headers.RetryAfter = retryAfter;
        }

        return Results.Json(new { error = new { message = "Stub failure.", type = "stub" } }, statusCode: (int)failure.Status);
    }

    private static float[] Vector(int index, int dimensions) => [.. Enumerable.Range(0, dimensions).Select(position => (float)(index + 1) / (position + 1))];

    public static JsonElement BodyOf(StubOpenAiRequest request) => JsonDocument.Parse(request.Body).RootElement.Clone();
}
