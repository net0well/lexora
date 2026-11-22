using System.Net;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Api.RateLimiting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Lexora.Api.IntegrationTests.Auth;

public sealed class AuthRateLimitingTests(ApiFactory api) : ApiTest(api)
{
    [Fact]
    public async Task Login_AboveThePerMinuteLimit_RespondsTooManyRequestsWithRetryAfter()
    {
        await using var limited = Api.WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:Auth:LoginPermitsPerMinute", "2"));
        using var client = limited.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var allowed = await LoginAsync(client);
            allowed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        using var rejected = await LoginAsync(client);

        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        rejected.Headers.RetryAfter.ShouldNotBeNull();
        (await rejected.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe(RateLimitRejectionWriter.Key);
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client) =>
        client.PostJsonAsync("/api/v1/auth/login", new { email = "nobody@lexora.test", password = "Wrong-Password-2026" }, CancellationToken);
}
