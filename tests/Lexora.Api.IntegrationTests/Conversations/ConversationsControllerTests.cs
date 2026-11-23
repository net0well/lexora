using System.Net;
using System.Net.Http.Headers;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Application.Common.Paging;
using Lexora.Application.Conversations.AskQuestion;
using Lexora.Application.Conversations.GetConversation;
using Lexora.Application.Conversations.ListConversations;
using Lexora.Application.Conversations.Services;
using Lexora.Application.Conversations.StartConversation;
using Lexora.Domain.Conversations;
using Lexora.Domain.Users;

namespace Lexora.Api.IntegrationTests.Conversations;

public sealed class ConversationsControllerTests(ApiFactory api) : ApiTest(api)
{
    [Fact]
    public async Task Start_ExistingBase_CreatesAConversationTheUserCanReadAndList()
    {
        var knowledgeBaseId = await IndexedBaseSeeder.SeedAsync(Api, CancellationToken);
        using var client = await ClientAsync(TestUsers.ViewerEmail);

        using var created = await client.PostJsonAsync("/api/v1/conversations", new { knowledgeBaseId, title = "Segurança" }, CancellationToken);

        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var conversation = (await created.ReadEnvelopeAsync<StartConversationResponse>(CancellationToken)).Data.ShouldNotBeNull();
        created.Headers.Location.ShouldBe(new Uri($"/api/v1/conversations/{conversation.Id}", UriKind.Relative));
        using var read = await client.GetAsync($"/api/v1/conversations/{conversation.Id}", CancellationToken);
        (await read.ReadEnvelopeAsync<GetConversationResponse>(CancellationToken)).Data!.Title.ShouldBe("Segurança");
        using var listed = await client.GetAsync($"/api/v1/conversations?knowledgeBaseId={knowledgeBaseId}", CancellationToken);
        (await listed.ReadEnvelopeAsync<PagedResponse<ConversationListItemResponse>>(CancellationToken)).Data!.Items
            .ShouldHaveSingleItem().Id.ShouldBe(conversation.Id);
    }

    [Fact]
    public async Task Ask_IndexedBase_AnswersWithCitationsAndKeepsTheExchange()
    {
        var knowledgeBaseId = await IndexedBaseSeeder.SeedAsync(
            Api,
            CancellationToken,
            ("Em caso de vazamento químico, isole a área num raio de 50 metros.", 12, "Emergência"),
            ("Após um vazamento químico, registre a ocorrência em 24 horas.", 13, "Emergência"));
        using var client = await ClientAsync(TestUsers.ViewerEmail);
        var conversationId = await StartAsync(client, knowledgeBaseId);

        using var response = await client.PostJsonAsync($"/api/v1/conversations/{conversationId}/messages", new { question = "O que fazer em vazamento químico?" }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var answer = (await response.ReadEnvelopeAsync<AskQuestionResponse>(CancellationToken)).Data.ShouldNotBeNull();
        answer.ShouldSatisfyAllConditions(
            body => body.Grounded.ShouldBeTrue(),
            body => (body.Provider, body.Model).ShouldBe(("fake", "fake-chat")),
            body => body.Citations.Select(citation => citation.Index).ShouldBe([1, 2]),
            body => body.Citations.ShouldAllBe(citation => citation.DocumentName == "manual.pdf" && citation.PageNumber >= 12));
        using var read = await client.GetAsync($"/api/v1/conversations/{conversationId}", CancellationToken);
        var messages = (await read.ReadEnvelopeAsync<GetConversationResponse>(CancellationToken)).Data!.Messages;
        messages.Select(message => message.Role).ShouldBe([MessageRole.User, MessageRole.Assistant]);
        messages[1].ShouldSatisfyAllConditions(
            message => message.Id.ShouldBe(answer.MessageId),
            message => message.Citations.Count.ShouldBe(2));
    }

    [Fact]
    public async Task Ask_BaseWithoutMatchingExcerpts_AnswersThatItFoundNothing()
    {
        var knowledgeBaseId = await IndexedBaseSeeder.SeedAsync(Api, CancellationToken);
        using var client = await ClientAsync(TestUsers.ViewerEmail);
        var conversationId = await StartAsync(client, knowledgeBaseId);

        using var response = await client.PostJsonAsync($"/api/v1/conversations/{conversationId}/messages", new { question = "Qual é o horário do refeitório?" }, CancellationToken);

        var answer = (await response.ReadEnvelopeAsync<AskQuestionResponse>(CancellationToken)).Data.ShouldNotBeNull();
        (answer.Answer, answer.Grounded, answer.Provider).ShouldBe((NoContextAnswer.Portuguese, false, AnswerService.NoContextProvider));
        answer.Citations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Conversation_OfAnotherUser_RespondsNotFoundEverywhere()
    {
        var knowledgeBaseId = await IndexedBaseSeeder.SeedAsync(Api, CancellationToken);
        using var owner = await ClientAsync(TestUsers.ViewerEmail);
        var conversationId = await StartAsync(owner, knowledgeBaseId);
        using var stranger = await ClientAsync(TestUsers.MemberEmail, UserRole.Member);

        using var read = await stranger.GetAsync($"/api/v1/conversations/{conversationId}", CancellationToken);
        using var ask = await stranger.PostJsonAsync($"/api/v1/conversations/{conversationId}/messages", new { question = "Qual o raio?" }, CancellationToken);
        using var delete = await stranger.DeleteAsync($"/api/v1/conversations/{conversationId}", CancellationToken);

        foreach (var response in new[] { read, ask, delete })
        {
            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("conversation.notFound");
        }
    }

    [Fact]
    public async Task Ask_EmptyQuestion_RespondsBadRequest()
    {
        using var client = await ClientAsync(TestUsers.ViewerEmail);

        using var response = await client.PostJsonAsync($"/api/v1/conversations/{Guid.CreateVersion7()}/messages", new { question = "  " }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("conversation.message.required");
    }

    [Fact]
    public async Task Delete_OwnConversation_RemovesIt()
    {
        var knowledgeBaseId = await IndexedBaseSeeder.SeedAsync(Api, CancellationToken);
        using var client = await ClientAsync(TestUsers.ViewerEmail);
        var conversationId = await StartAsync(client, knowledgeBaseId);

        using var deleted = await client.DeleteAsync($"/api/v1/conversations/{conversationId}", CancellationToken);
        using var read = await client.GetAsync($"/api/v1/conversations/{conversationId}", CancellationToken);

        deleted.StatusCode.ShouldBe(HttpStatusCode.OK);
        read.StatusCode.ShouldBe(HttpStatusCode.NotFound);
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
