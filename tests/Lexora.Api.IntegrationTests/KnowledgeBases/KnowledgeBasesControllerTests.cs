using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Application.Common.Paging;
using Lexora.Application.KnowledgeBases.CreateKnowledgeBase;
using Lexora.Application.KnowledgeBases.GetKnowledgeBase;
using Lexora.Application.KnowledgeBases.ListKnowledgeBases;
using Lexora.Application.KnowledgeBases.UpdateKnowledgeBase;
using Lexora.Domain.KnowledgeBases;
using Lexora.Domain.Users;

namespace Lexora.Api.IntegrationTests.KnowledgeBases;

public sealed class KnowledgeBasesControllerTests(ApiFactory api) : ApiTest(api)
{
    private const string BasePath = "/api/v1/knowledge-bases";

    [Fact]
    public async Task Create_ValidBase_RespondsCreatedWithLocationAndDefaults()
    {
        using var client = await AdminClientAsync();

        using var response = await client.PostJsonAsync(BasePath, new { name = "Safety Procedures", description = "Plant manuals" }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await response.ReadEnvelopeAsync<CreateKnowledgeBaseResponse>(CancellationToken)).Data.ShouldNotBeNull();
        created.Name.ShouldBe("Safety Procedures");
        created.ChunkMaxTokens.ShouldBe(ChunkingSettings.DefaultMaxTokens);
        created.ChunkOverlapTokens.ShouldBe(ChunkingSettings.DefaultOverlapTokens);
        response.Headers.Location.ShouldBe(new Uri($"{BasePath}/{created.Id}", UriKind.Relative));
    }

    [Fact]
    public async Task Create_NameAlreadyUsedIgnoringCase_RespondsConflict()
    {
        using var client = await AdminClientAsync();
        await CreateAsync(client, "Safety Procedures");

        using var response = await client.PostJsonAsync(BasePath, new { name = "SAFETY procedures" }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("knowledgeBase.name.duplicate");
    }

    [Fact]
    public async Task Create_InvalidInput_RespondsBadRequestWithEveryKey()
    {
        using var client = await AdminClientAsync();

        using var response = await client.PostJsonAsync(BasePath, new { name = "ab", description = new string('d', 1001), chunkMaxTokens = 50 }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Select(notification => notification.Key).ShouldBe(
            ["knowledgeBase.name.tooShort", "knowledgeBase.description.tooLong", "knowledgeBase.chunking.invalid"],
            ignoreOrder: true);
    }

    [Fact]
    public async Task Get_ExistingBase_ReturnsItsDetails()
    {
        using var client = await AdminClientAsync();
        var created = await CreateAsync(client, "Safety Procedures", chunkMaxTokens: 600, chunkOverlapTokens: 60);

        using var response = await client.GetAsync($"{BasePath}/{created.Id}", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var knowledgeBase = (await response.ReadEnvelopeAsync<GetKnowledgeBaseResponse>(CancellationToken)).Data.ShouldNotBeNull();
        knowledgeBase.Status.ShouldBe(KnowledgeBaseStatus.Active);
        knowledgeBase.ChunkMaxTokens.ShouldBe(600);
        knowledgeBase.ChunkOverlapTokens.ShouldBe(60);
        knowledgeBase.DocumentCount.ShouldBe(0);
    }

    [Fact]
    public async Task Get_UnknownBase_RespondsNotFound()
    {
        using var client = await AdminClientAsync();

        using var response = await client.GetAsync($"{BasePath}/{Guid.CreateVersion7()}", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("knowledgeBase.notFound");
    }

    [Fact]
    public async Task Update_NewValues_RespondsWithTheUpdatedBase()
    {
        using var client = await AdminClientAsync();
        var created = await CreateAsync(client, "Safety Procedures", chunkMaxTokens: 600, chunkOverlapTokens: 60);

        using var response = await client.PutAsJsonAsync(
            $"{BasePath}/{created.Id}",
            new { name = "Safety Manuals", description = "Revised", chunkOverlapTokens = 100 },
            ApiJson.Options,
            CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = (await response.ReadEnvelopeAsync<UpdateKnowledgeBaseResponse>(CancellationToken)).Data.ShouldNotBeNull();
        updated.Name.ShouldBe("Safety Manuals");
        updated.Description.ShouldBe("Revised");
        updated.ChunkMaxTokens.ShouldBe(600);
        updated.ChunkOverlapTokens.ShouldBe(100);
    }

    [Fact]
    public async Task Archive_ActiveBase_IsListedAsArchived()
    {
        using var client = await AdminClientAsync();
        var created = await CreateAsync(client, "Safety Procedures");
        await CreateAsync(client, "Quality Manuals");

        using var archive = await client.PostAsync($"{BasePath}/{created.Id}/archive", content: null, CancellationToken);
        var archived = await ListAsync(client, "?status=archived");
        var active = await ListAsync(client, "?status=active");

        archive.StatusCode.ShouldBe(HttpStatusCode.OK);
        archived.Items.ShouldHaveSingleItem().Id.ShouldBe(created.Id);
        active.Items.ShouldHaveSingleItem().Name.ShouldBe("Quality Manuals");
    }

    [Fact]
    public async Task List_Paging_ReturnsThePageAndTotals()
    {
        using var client = await AdminClientAsync();
        foreach (var name in new[] { "Alpha Base", "Bravo Base", "Charlie Base" })
        {
            await CreateAsync(client, name);
        }

        var page = await ListAsync(client, "?page=2&pageSize=2");

        page.Items.ShouldHaveSingleItem().Name.ShouldBe("Charlie Base");
        page.TotalItems.ShouldBe(3);
        page.TotalPages.ShouldBe(2);
    }

    [Theory]
    [InlineData("?pageSize=0", "paging.pageSize.invalid")]
    [InlineData("?page=0", "paging.page.invalid")]
    [InlineData("?status=deleted", "request.malformed")]
    public async Task List_InvalidQuery_RespondsBadRequest(string query, string expectedKey)
    {
        using var client = await AdminClientAsync();

        using var response = await client.GetAsync(BasePath + query, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe(expectedKey);
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var admin = await Api.SeedUserAsync(TestUsers.AdminEmail, UserRole.Admin, TestUsers.Password);
        var client = Api.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Api.IssueAccessToken(admin));

        return client;
    }

    private static async Task<CreateKnowledgeBaseResponse> CreateAsync(HttpClient client, string name, int? chunkMaxTokens = null, int? chunkOverlapTokens = null)
    {
        using var response = await client.PostJsonAsync(BasePath, new { name, chunkMaxTokens, chunkOverlapTokens }, CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.ReadEnvelopeAsync<CreateKnowledgeBaseResponse>(CancellationToken)).Data.ShouldNotBeNull();
    }

    private static async Task<PagedResponse<KnowledgeBaseListItemResponse>> ListAsync(HttpClient client, string query)
    {
        using var response = await client.GetAsync(BasePath + query, CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return (await response.ReadEnvelopeAsync<PagedResponse<KnowledgeBaseListItemResponse>>(CancellationToken)).Data.ShouldNotBeNull();
    }
}
