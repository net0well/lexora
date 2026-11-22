using System.Net;
using Lexora.Api.IntegrationTests.Hosting;
using Lexora.Domain.Users;

namespace Lexora.Api.IntegrationTests.Auth;

public sealed class AuthControllerPasswordTests(ApiFactory api) : ApiTest(api)
{
    private const string ChangePasswordPath = "/api/v1/auth/change-password";
    private const string NewPassword = "Brand-New-Password-2026";

    [Fact]
    public async Task ChangePassword_RightCurrentPassword_SwitchesTheCredentialsAndEndsOtherSessions()
    {
        await Api.SeedUserAsync(TestUsers.MemberEmail, UserRole.Member, TestUsers.Password);
        using var otherDevice = Api.CreateApiClient(handleCookies: false);
        using var otherLogin = await otherDevice.PostJsonAsync(
            "/api/v1/auth/login",
            new { email = TestUsers.MemberEmail, password = TestUsers.Password },
            CancellationToken);
        var otherCookie = RefreshCookie.ValueOf(otherLogin).ShouldNotBeNull();
        using var client = Api.CreateApiClient();
        await client.SignInAsync(TestUsers.MemberEmail, TestUsers.Password, CancellationToken);

        using var response = await client.PostJsonAsync(
            ChangePasswordPath,
            new { currentPassword = TestUsers.Password, newPassword = NewPassword },
            CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await LoginStatusAsync(TestUsers.Password)).ShouldBe(HttpStatusCode.Unauthorized);
        (await LoginStatusAsync(NewPassword)).ShouldBe(HttpStatusCode.OK);
        using var refresh = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        RefreshCookie.Attach(refresh.Headers, otherCookie);
        (await otherDevice.SendAsync(refresh, CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Wrong-Password-2026", NewPassword, "auth.password.currentInvalid")]
    [InlineData(TestUsers.Password, "weak", "auth.password.weak")]
    public async Task ChangePassword_InvalidInput_RespondsBadRequestAndKeepsThePassword(string current, string next, string expectedKey)
    {
        await Api.SeedUserAsync(TestUsers.MemberEmail, UserRole.Member, TestUsers.Password);
        using var client = Api.CreateApiClient();
        await client.SignInAsync(TestUsers.MemberEmail, TestUsers.Password, CancellationToken);

        using var response = await client.PostJsonAsync(ChangePasswordPath, new { currentPassword = current, newPassword = next }, CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadEnvelopeAsync(CancellationToken)).Notifications.ShouldContain(notification => notification.Key == expectedKey);
        (await LoginStatusAsync(TestUsers.Password)).ShouldBe(HttpStatusCode.OK);
    }

    private async Task<HttpStatusCode> LoginStatusAsync(string password)
    {
        using var client = Api.CreateApiClient();
        using var response = await client.PostJsonAsync("/api/v1/auth/login", new { email = TestUsers.MemberEmail, password }, CancellationToken);

        return response.StatusCode;
    }
}
