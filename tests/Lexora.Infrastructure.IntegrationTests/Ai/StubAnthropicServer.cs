using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Lexora.Infrastructure.IntegrationTests.Ai;

public sealed class StubAnthropicServer : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly ConcurrentQueue<(HttpStatusCode Status, string Type)> _failures = new();

    private StubAnthropicServer(WebApplication app) => _app = app;

    public ConcurrentQueue<StubOpenAiRequest> Requests { get; } = new();

    public Uri BaseAddress { get; private set; } = null!;

    public static async Task<StubAnthropicServer> StartAsync(CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var server = new StubAnthropicServer(builder.Build());
        server._app.MapPost("/v1/messages", (Func<HttpContext, Task<IResult>>)server.MessagesAsync);

        await server._app.StartAsync(cancellationToken);
        BaseAddressOf(server);

        return server;
    }

    public void FailNext(HttpStatusCode status, string type) => _failures.Enqueue((status, type));

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    private static void BaseAddressOf(StubAnthropicServer server) =>
        server.BaseAddress = new Uri(server._app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First());

    private async Task<IResult> MessagesAsync(HttpContext context)
    {
        var body = (await JsonNode.ParseAsync(context.Request.Body))!.AsObject();
        Requests.Enqueue(new StubOpenAiRequest(context.Request.Path, context.Request.Headers.Authorization.ToString(), body.ToJsonString()));

        if (_failures.TryDequeue(out var failure))
        {
            return Results.Json(new { type = "error", error = new { type = failure.Type, message = "Stub failure." } }, statusCode: (int)failure.Status);
        }

        return Results.Json(new
        {
            id = "msg_1",
            type = "message",
            role = "assistant",
            model = body["model"]!.GetValue<string>(),
            content = new[] { new { type = "text", text = "Interns get 15 days [2]." } },
            stop_reason = "end_turn",
            stop_sequence = (string?)null,
            usage = new { input_tokens = 12, output_tokens = 5 },
        });
    }
}
