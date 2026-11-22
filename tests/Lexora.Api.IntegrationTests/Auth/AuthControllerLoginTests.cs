using System.Net;
using System.Net.Http.Headers;
using Lexora.Api.Contracts.Responses;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Application.Auth;
using Lexora.Domain.Users;

namespace Lexora.Api.IntegrationTests.Auth;

public sealed class AuthControllerLoginTests(ApiFactory api) : ApiTest(api)
{
    private const string LoginPath = "/api/v1/auth/login";

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTheSessionAndAHardenedRefreshCookie()
    {
        var user = await Api.SeedUserAsync(TestUsers.MemberEmail, UserRole.Member, TestUsers.Password);
        using var client = Api.CreateApiClient(handleCookies: false);

        using var response = await LoginAsync(client, TestUsers.MemberEmail, TestUsers.Password);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var envelope = await response.ReadEnvelopeAsync<SessionResponse>(CancellationToken);
        envelope.Success.ShouldBeTrue();
        envelope.Data.ShouldNotBeNull().User.Id.ShouldBe(user.Id);
        envelope.Data.AccessToken.ShouldNotBeNullOrWhiteSpace();
        (await response.Content.ReadAsStringAsync(CancellationToken)).ShouldNotContain("refreshToken");
        var cookie = RefreshCookie.HeaderOf(response).ShouldNotBeNull();
        cookie.ShouldContain("httponly");
        cookie.ShouldContain("secure");
        cookie.ShouldContain("samesite=strict");
        cookie.ShouldContain("path=/api/v1/auth");
    }

    [Theory]
    [InlineData(TestUsers.MemberEmail, "Wrong-Password-2026")]
    [InlineData("nobody@lexora.test", TestUsers.Password)]
    public async Task Login_WrongPasswordOrUnknownEmail_RespondsTheSameUnauthorized(string email, string password)
    {
        await Api.SeedUserAsync(TestUsers.MemberEmail, UserRole.Member, TestUsers.Password);
        using var client = Api.CreateApiClient();

        using var response = await LoginAsync(client, email, password);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.ShouldHaveSingleItem().Key.ShouldBe(AuthNotifications.InvalidCredentials.Key);
        RefreshCookie.HeaderOf(response).ShouldBeNull();
    }

    [Fact]
    public async Task Login_InactiveUser_RespondsInvalidCredentials()
    {
        await Api.SeedUserAsync(TestUsers.MemberEmail, UserRole.Member, TestUsers.Password, active: false);
        using var client = Api.CreateApiClient();

        using var response = await LoginAsync(client, TestUsers.MemberEmail, TestUsers.Password);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe(AuthNotifications.InvalidCredentials.Key);
    }

    [Fact]
    public async Task Login_FifthWrongPassword_LocksTheAccountEvenForTheRightPassword()
    {
        await Api.SeedUserAsync(TestUsers.MemberEmail, UserRole.Member, TestUsers.Password);
        using var client = Api.CreateApiClient();

        for (var attempt = 1; attempt < 5; attempt++)
        {
            using var failed = await LoginAsync(client, TestUsers.MemberEmail, "Wrong-Password-2026");
            (await failed.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe(AuthNotifications.InvalidCredentials.Key);
        }

        using var fifth = await LoginAsync(client, TestUsers.MemberEmail, "Wrong-Password-2026");
        using var rightPassword = await LoginAsync(client, TestUsers.MemberEmail, TestUsers.Password);

        (await fifth.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe(AuthNotifications.LockedOut.Key);
        rightPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await rightPassword.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe(AuthNotifications.LockedOut.Key);
    }

    [Fact]
    public async Task Login_MissingPassword_RespondsBadRequestWithTheValidationKey()
    {
        using var client = Api.CreateApiClient();

        using var response = await client.PostJsonAsync(LoginPath, new { email = TestUsers.MemberEmail }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.ShouldHaveSingleItem().Key.ShouldBe("auth.password.required");
    }

    [Fact]
    public async Task Login_MalformedBody_RespondsBadRequestEnvelope()
    {
        using var client = Api.CreateApiClient();
        using var content = new StringContent("{\"email\":", MediaTypeHeaderValue.Parse("application/json"));

        using var response = await client.PostAsync(LoginPath, content, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var envelope = await response.ReadEnvelopeAsync(CancellationToken);
        envelope.Success.ShouldBeFalse();
        envelope.Notifications.Single().Key.ShouldBe("request.malformed");
    }

    [Fact]
    public async Task Me_SignedIn_ReturnsTheCurrentUser()
    {
        var user = await Api.SeedUserAsync(TestUsers.ViewerEmail, UserRole.Viewer, TestUsers.Password);
        using var client = Api.CreateApiClient();
        await client.SignInAsync(TestUsers.ViewerEmail, TestUsers.Password, CancellationToken);

        using var response = await client.GetAsync("/api/v1/auth/me", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.ReadEnvelopeAsync<AuthenticatedUserResponse>(CancellationToken)).Data
            .ShouldBe(new AuthenticatedUserResponse(user.Id, TestUsers.ViewerEmail, user.Name.Value, UserRole.Viewer));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-jwt")]
    public async Task Me_WithoutValidToken_RespondsUnauthorizedEnvelope(string? token)
    {
        using var client = Api.CreateApiClient();

        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await client.GetAsync("/api/v1/auth/me", CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ShouldContain(header => header.Scheme == "Bearer");
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.Single().Key.ShouldBe("auth.unauthenticated");
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        client.PostJsonAsync(LoginPath, new { email, password }, CancellationToken);
}
