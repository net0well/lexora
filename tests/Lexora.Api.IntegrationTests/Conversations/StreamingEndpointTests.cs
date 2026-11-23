using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Application.Conversations.GetConversation;
using Lexora.Application.Conversations.StartConversation;
using Lexora.Domain.Users;

namespace Lexora.Api.IntegrationTests.Conversations;

public sealed class StreamingEndpointTests(ApiFactory api) : ApiTest(api)
{
    [Fact]
    public async Task Stream_IndexedBase_SendsTheAnswerInPiecesThenCitationsThenDoneAndRecordsIt()
    {
        var knowledgeBaseId = await IndexedBaseSeeder.SeedAsync(
            Api,
            CancellationToken,
            ("Em caso de vazamento químico, isole a área num raio de 50 metros.", 12, "Emergência"),
            ("Após um vazamento químico, registre a ocorrência em 24 horas.", 13, "Emergência"));
        using var client = await ClientAsync(TestUsers.ViewerEmail);
        var conversationId = await StartAsync(client, knowledgeBaseId);

        using var response = await StreamAsync(client, conversationId, "O que fazer em vazamento químico?");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/event-stream");
        var events = await ReadEventsAsync(response);
        var names = events.Select(item => item.Name).ToList();
        names.Take(names.Count - 2).ShouldAllBe(name => name == "delta");
        names.TakeLast(2).ShouldBe(["citations", "done"]);
        var answer = string.Concat(events.Where(item => item.Name == "delta").Select(item => item.Data.GetProperty("text").GetString()));
        answer.ShouldEndWith("based on the excerpts [1] and [2].");
        var citations = events.Single(item => item.Name == "citations").Data;
        citations.GetProperty("grounded").GetBoolean().ShouldBeTrue();
        citations.GetProperty("citations").GetArrayLength().ShouldBe(2);
        var done = events.Single(item => item.Name == "done").Data;
        done.GetProperty("provider").GetString().ShouldBe("fake");
        using var read = await client.GetAsync($"/api/v1/conversations/{conversationId}", CancellationToken);
        var messages = (await read.ReadEnvelopeAsync<GetConversationResponse>(CancellationToken)).Data!.Messages;
        messages.Select(message => message.Content).ShouldBe(["O que fazer em vazamento químico?", answer]);
        messages[1].Id.ShouldBe(done.GetProperty("messageId").GetGuid());
    }

    [Fact]
    public async Task Stream_ConversationOfAnotherUser_RespondsNotFoundBeforeStreaming()
    {
        var knowledgeBaseId = await IndexedBaseSeeder.SeedAsync(Api, CancellationToken);
        using var owner = await ClientAsync(TestUsers.ViewerEmail);
        var conversationId = await StartAsync(owner, knowledgeBaseId);
        using var stranger = await ClientAsync(TestUsers.MemberEmail, UserRole.Member);

        using var response = await StreamAsync(stranger, conversationId, "Qual o raio?");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("conversation.notFound");
    }

    [Fact]
    public async Task Stream_EmptyQuestion_RespondsBadRequestBeforeStreaming()
    {
        var knowledgeBaseId = await IndexedBaseSeeder.SeedAsync(Api, CancellationToken);
        using var client = await ClientAsync(TestUsers.ViewerEmail);
        var conversationId = await StartAsync(client, knowledgeBaseId);

        using var response = await StreamAsync(client, conversationId, " ");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("conversation.message.required");
    }

    private static Task<HttpResponseMessage> StreamAsync(HttpClient client, Guid conversationId, string question)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/conversations/{conversationId}/messages/stream")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { question }, options: ApiJson.Options),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        return client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, CancellationToken);
    }

    private static async Task<List<(string Name, JsonElement Data)>> ReadEventsAsync(HttpResponseMessage response)
    {
        var body = (await response.Content.ReadAsStringAsync(CancellationToken)).ReplaceLineEndings("\n");

        return
        [
            .. body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries).Select(block =>
            {
                var lines = block.Split('\n');
                var name = lines.Single(line => line.StartsWith("event: ", StringComparison.Ordinal))["event: ".Length..];
                var data = lines.Single(line => line.StartsWith("data: ", StringComparison.Ordinal))["data: ".Length..];

                return (name, JsonDocument.Parse(data).RootElement.Clone());
            }),
        ];
    }

    private async Task<HttpClient> ClientAsync(string email, UserRole role = UserRole.Viewer)
    {
        var user = await Api.SeedUserAsync(email, role, TestUsers.Password);
        var client = Api.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Api.IssueAccessToken(user));

        return client;
    }

    private static async Task<Guid> StartAsync(HttpClient client, Guid knowledgeBaseId)
    {
        using var response = await client.PostJsonAsync("/api/v1/conversations", new { knowledgeBaseId }, CancellationToken);

        return (await response.ReadEnvelopeAsync<StartConversationResponse>(CancellationToken)).Data!.Id;
    }
}
