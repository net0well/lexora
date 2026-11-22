using Lexora.Application.Auth;
using Lexora.Application.Auth.ChangePassword;
using Lexora.Application.Auth.Logout;
using Lexora.Domain.RefreshTokens;
using Lexora.Shared.Results;

namespace Lexora.Application.UnitTests.Auth;

public sealed class AuthServiceAccountTests
{
    private const string NewPassword = "An0therStrongOne";

    private readonly AuthServiceHarness _harness = new();

    [Fact]
    public async Task LogoutAsync_CurrentSessionToken_RevokesIt()
    {
        var user = await _harness.AddUserAsync();
        var session = await _harness.SignInAsync(user);

        var result = await _harness.Service.LogoutAsync(new LogoutCommand(session.RefreshToken), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        _harness.TokenFor(session.RefreshToken).RevocationReason.ShouldBe(RefreshTokenRevocationReason.Logout);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("unknown-token")]
    public async Task LogoutAsync_MissingOrUnknownToken_SucceedsWithoutChanges(string? refreshToken)
    {
        var user = await _harness.AddUserAsync();
        var session = await _harness.SignInAsync(user);

        (await _harness.Service.LogoutAsync(new LogoutCommand(refreshToken), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();

        _harness.TokenFor(session.RefreshToken).RevokedAt.ShouldBeNull();
    }

    [Fact]
    public async Task LogoutAsync_TokenOfAnotherUser_KeepsThatSessionOpen()
    {
        var owner = await _harness.AddUserAsync("owner@lexora.local");
        var ownerSession = await _harness.SignInAsync(owner);
        var intruder = await _harness.AddUserAsync("intruder@lexora.local");
        await _harness.SignInAsync(intruder);

        await _harness.Service.LogoutAsync(new LogoutCommand(ownerSession.RefreshToken), TestContext.Current.CancellationToken);

        _harness.TokenFor(ownerSession.RefreshToken).RevokedAt.ShouldBeNull();
    }

    [Fact]
    public async Task ChangePasswordAsync_RightCurrentPassword_ReplacesItAndClosesEverySession()
    {
        var user = await _harness.AddUserAsync();
        var first = await _harness.SignInAsync(user);
        var second = await _harness.SignInAsync(user);
        _harness.Time.Advance(TimeSpan.FromHours(1));

        var result = await ChangePasswordAsync(AuthServiceHarness.Password, NewPassword);

        result.IsSuccess.ShouldBeTrue();
        user.PasswordHash.ShouldBe(_harness.Hasher.Hash(NewPassword));
        user.PasswordChangedAt.ShouldBe(_harness.Time.GetUtcNow());
        _harness.TokenFor(first.RefreshToken).RevocationReason.ShouldBe(RefreshTokenRevocationReason.PasswordChanged);
        _harness.TokenFor(second.RefreshToken).RevocationReason.ShouldBe(RefreshTokenRevocationReason.PasswordChanged);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_KeepsThePassword()
    {
        var user = await _harness.AddUserAsync();
        await _harness.SignInAsync(user);

        var result = await ChangePasswordAsync("WrongPassw0rd", NewPassword);

        result.Notifications.ShouldBe([AuthNotifications.CurrentPasswordInvalid]);
        user.PasswordHash.ShouldBe(_harness.Hasher.Hash(AuthServiceHarness.Password));
    }

    [Fact]
    public async Task ChangePasswordAsync_NewPasswordContainingTheEmail_FailsAsWeak()
    {
        var user = await _harness.AddUserAsync("ana@lexora.local");
        await _harness.SignInAsync(user);

        var result = await ChangePasswordAsync(AuthServiceHarness.Password, "X1ana@lexora.local");

        result.Notifications.Single().Key.ShouldBe("auth.password.weak");
    }

    private Task<Result> ChangePasswordAsync(string currentPassword, string newPassword) =>
        _harness.Service.ChangePasswordAsync(new ChangePasswordCommand(currentPassword, newPassword), TestContext.Current.CancellationToken);
}
