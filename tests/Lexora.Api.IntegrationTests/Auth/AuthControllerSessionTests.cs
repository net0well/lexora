using System.Net;
using System.Net.Http.Headers;
using Lexora.Api.Contracts.Responses;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Application.Auth;
using Lexora.Domain.Users;

namespace Lexora.Api.IntegrationTests.Auth;

public sealed class AuthControllerSessionTests(ApiFactory api) : ApiTest(api)
{
    private const string RefreshPath = "/api/v1/auth/refresh";

    [Fact]
    public async Task Refresh_ActiveCookie_IssuesANewAccessTokenAndRotatesTheCookie()
    {
        await Api.SeedUserAsync(TestUsers.MemberEmail, UserRole.Member, TestUsers.Password);
        using var client = Api.CreateApiClient(handleCookies: false);
        var cookie = await SignInAsync(client);

        using var response = await RefreshAsync(client, cookie);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadEnvelopeAsync<SessionResponse>(CancellationToken)).Data.ShouldNotBeNull().AccessToken.ShouldNotBeNullOrWhiteSpace();
        RefreshCookie.ValueOf(response).ShouldNotBeNull().ShouldNotBe(cookie);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_RespondsInvalidRefreshToken()
    {
        using var client = Api.CreateApiClient(handleCookies: false);

        using var response = await client.PostAsync(RefreshPath, content: null, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe(AuthNotifications.InvalidRefreshToken.Key);
    }

    [Fact]
    public async Task Refresh_ReusedCookie_RevokesTheSessionAndClearsTheCookie()
    {
        await Api.SeedUserAsync(TestUsers.MemberEmail, UserRole.Member, TestUsers.Password);
        using var client = Api.CreateApiClient(handleCookies: false);
        var original = await SignInAsync(client);
        using var rotation = await RefreshAsync(client, original);
        var rotated = RefreshCookie.ValueOf(rotation).ShouldNotBeNull();

        using var reuse = await RefreshAsync(client, original);
        using var afterReuse = await RefreshAsync(client, rotated);

        reuse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        RefreshCookie.HeaderOf(reuse).ShouldNotBeNull().ShouldContain("expires=Thu, 01 Jan 1970");
        afterReuse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_SignedIn_ClearsTheCookieAndEndsTheSession()
    {
        await Api.SeedUserAsync(TestUsers.MemberEmail, UserRole.Member, TestUsers.Password);
        using var client = Api.CreateApiClient(handleCookies: false);
        var cookie = await SignInAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        RefreshCookie.Attach(request.Headers, cookie);

        using var response = await client.SendAsync(request, CancellationToken);
        using var refresh = await RefreshAsync(client, cookie);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var envelope = await response.ReadEnvelopeAsync(CancellationToken);
        envelope.Success.ShouldBeTrue();
        envelope.Data.ShouldBeNull();
        RefreshCookie.HeaderOf(response).ShouldNotBeNull().ShouldContain("expires=Thu, 01 Jan 1970");
        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithoutAccessToken_RespondsUnauthorized()
    {
        using var client = Api.CreateApiClient();

        using var response = await client.PostAsync("/api/v1/auth/logout", content: null, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_UserDeactivatedByAnAdmin_EndsTheSession()
    {
        await Api.SeedUserAsync(TestUsers.AdminEmail, UserRole.Admin, TestUsers.Password);
        var member = await Api.SeedUserAsync(TestUsers.MemberEmail, UserRole.Member, TestUsers.Password);
        using var memberClient = Api.CreateApiClient(handleCookies: false);
        var cookie = await SignInAsync(memberClient);
        using var adminClient = Api.CreateApiClient();
        await adminClient.SignInAsync(TestUsers.AdminEmail, TestUsers.Password, CancellationToken);

        using var deactivation = await adminClient.PostAsync($"/api/v1/users/{member.Id}/deactivate", content: null, CancellationToken);
        using var refresh = await RefreshAsync(memberClient, cookie);

        deactivation.StatusCode.ShouldBe(HttpStatusCode.OK);
        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task<string> SignInAsync(HttpClient client)
    {
        using var response = await client.PostJsonAsync(
            "/api/v1/auth/login",
            new { email = TestUsers.MemberEmail, password = TestUsers.Password },
            CancellationToken);

        var session = (await response.ReadEnvelopeAsync<SessionResponse>(CancellationToken)).Data.ShouldNotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        return RefreshCookie.ValueOf(response).ShouldNotBeNull();
    }

    private static async Task<HttpResponseMessage> RefreshAsync(HttpClient client, string cookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, RefreshPath);
        RefreshCookie.Attach(request.Headers, cookie);

        return await client.SendAsync(request, CancellationToken);
    }
}
