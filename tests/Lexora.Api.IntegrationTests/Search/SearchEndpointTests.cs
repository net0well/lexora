using System.Net;
using System.Net.Http.Headers;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Application.Retrieval.Fusion;
using Lexora.Application.Retrieval.SearchKnowledgeBase;
using Lexora.Domain.Users;

namespace Lexora.Api.IntegrationTests.Search;

public sealed class SearchEndpointTests(ApiFactory api) : ApiTest(api)
{
    [Fact]
    public async Task Search_IndexedBase_ReturnsTheMatchingExcerptWithItsLocation()
    {
        var knowledgeBaseId = await SeedIndexedBaseAsync(
            ("Em caso de vazamento químico, isole a área num raio de 50 metros.", 12, "Procedimentos de emergência"),
            ("Use luvas nitrílicas ao manusear solventes.", 3, "Equipamentos"),
            ("O refeitório funciona das 11h às 14h.", 20, null));
        using var client = await ViewerClientAsync();

        using var response = await client.PostJsonAsync($"/api/v1/knowledge-bases/{knowledgeBaseId}/search", new { query = "vazamento químico" }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var hits = (await response.ReadEnvelopeAsync<SearchKnowledgeBaseResponse>(CancellationToken)).Data.ShouldNotBeNull().Hits;
        hits[0].ShouldSatisfyAllConditions(
            hit => hit.Content.ShouldStartWith("Em caso de vazamento químico"),
            hit => hit.PageNumber.ShouldBe(12),
            hit => hit.SectionTitle.ShouldBe("Procedimentos de emergência"),
            hit => hit.DocumentName.ShouldBe("manual.pdf"),
            hit => hit.MatchedBy.ShouldBe(SearchMatch.Both));
    }

    [Fact]
    public async Task Search_KeywordMode_FindsTheExactTermOnly()
    {
        var knowledgeBaseId = await SeedIndexedBaseAsync(
            ("Instalações elétricas seguem a norma NR-10.", 1, null),
            ("Máquinas seguem a norma NR-12.", 2, null));
        using var client = await ViewerClientAsync();

        using var response = await client.PostJsonAsync(
            $"/api/v1/knowledge-bases/{knowledgeBaseId}/search", new { query = "NR-10", mode = "keyword" }, CancellationToken);

        (await response.ReadEnvelopeAsync<SearchKnowledgeBaseResponse>(CancellationToken)).Data!.Hits
            .ShouldHaveSingleItem().Content.ShouldBe("Instalações elétricas seguem a norma NR-10.");
    }

    [Fact]
    public async Task Search_UnknownBase_RespondsNotFound()
    {
        using var client = await ViewerClientAsync();

        using var response = await client.PostJsonAsync($"/api/v1/knowledge-bases/{Guid.CreateVersion7()}/search", new { query = "raio" }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("knowledgeBase.notFound");
    }

    [Fact]
    public async Task Search_EmptyQuery_RespondsBadRequest()
    {
        using var client = await ViewerClientAsync();

        using var response = await client.PostJsonAsync($"/api/v1/knowledge-bases/{Guid.CreateVersion7()}/search", new { query = " " }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("search.query.required");
    }

    private async Task<HttpClient> ViewerClientAsync()
    {
        var viewer = await Api.SeedUserAsync(TestUsers.ViewerEmail, UserRole.Viewer, TestUsers.Password);
        var client = Api.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Api.IssueAccessToken(viewer));

        return client;
    }

    private Task<Guid> SeedIndexedBaseAsync(params (string Content, int Page, string? Section)[] excerpts) =>
        IndexedBaseSeeder.SeedAsync(Api, CancellationToken, excerpts);
}
