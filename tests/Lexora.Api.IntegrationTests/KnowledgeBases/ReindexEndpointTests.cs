using System.Net;
using System.Net.Http.Headers;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Application.KnowledgeBases.ReindexKnowledgeBase;
using Lexora.Domain.Documents;
using Lexora.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Api.IntegrationTests.KnowledgeBases;

public sealed class ReindexEndpointTests(ApiFactory api) : ApiTest(api)
{
    [Fact]
    public async Task Reindex_IndexedBase_AcceptsAndSendsItsDocumentsBackThroughThePipeline()
    {
        var knowledgeBaseId = await IndexedBaseSeeder.SeedAsync(Api, CancellationToken, ("Isole a área num raio de 50 metros.", 1, null));
        using var client = await AdminClientAsync();

        using var response = await client.PostAsync($"/api/v1/knowledge-bases/{knowledgeBaseId}/reindex", content: null, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Headers.Location.ShouldBe(new Uri($"/api/v1/knowledge-bases/{knowledgeBaseId}", UriKind.Relative));
        (await response.ReadEnvelopeAsync<ReindexKnowledgeBaseResponse>(CancellationToken)).Data.ShouldBe(new ReindexKnowledgeBaseResponse(knowledgeBaseId, 1));
        await using var context = Api.CreateDbContext();
        var document = await context.Documents.SingleAsync(document => document.KnowledgeBaseId == knowledgeBaseId, CancellationToken);
        (document.Status, document.ProcessingVersion).ShouldBe((DocumentStatus.Uploaded, 2));
    }

    [Fact]
    public async Task Reindex_UnknownBase_RespondsNotFound()
    {
        using var client = await AdminClientAsync();

        using var response = await client.PostAsync($"/api/v1/knowledge-bases/{Guid.CreateVersion7()}/reindex", content: null, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var admin = await Api.SeedUserAsync(TestUsers.AdminEmail, UserRole.Admin, TestUsers.Password);
        var client = Api.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Api.IssueAccessToken(admin));

        return client;
    }
}
