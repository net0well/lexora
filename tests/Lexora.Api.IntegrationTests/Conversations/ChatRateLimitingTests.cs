using System.Net;
using System.Net.Http.Headers;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Api.RateLimiting;
using Lexora.Domain.Users;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lexora.Api.IntegrationTests.Conversations;

public sealed class ChatRateLimitingTests(ApiFactory api) : ApiTest(api)
{
    [Fact]
    public async Task Ask_AboveThePerMinuteLimit_RejectsThatUserOnly()
    {
        await using var limited = Api.WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:Chat:PermitsPerMinute", "2"));
        using var first = await ClientAsync(limited, TestUsers.ViewerEmail);
        using var second = await ClientAsync(limited, TestUsers.MemberEmail);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var allowed = await AskAsync(first, streaming: attempt == 1);
            allowed.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        using var rejected = await AskAsync(first, streaming: false);
        using var otherUser = await AskAsync(second, streaming: false);

        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        rejected.Headers.RetryAfter.ShouldNotBeNull();
        (await rejected.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe(RateLimitRejectionWriter.Key);
        otherUser.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<HttpClient> ClientAsync(WebApplicationFactory<Program> factory, string email)
    {
        var user = await Api.SeedUserAsync(email, UserRole.Viewer, TestUsers.Password);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Api.IssueAccessToken(user));

        return client;
    }

    private static Task<HttpResponseMessage> AskAsync(HttpClient client, bool streaming) =>
        client.PostJsonAsync(
            $"/api/v1/conversations/{Guid.CreateVersion7()}/messages{(streaming ? "/stream" : string.Empty)}",
            new { question = "Qual o raio?" },
            CancellationToken);
}
