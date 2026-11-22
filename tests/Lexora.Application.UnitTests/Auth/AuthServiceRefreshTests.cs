using Lexora.Application.Auth;
using Lexora.Application.Auth.Login;
using Lexora.Application.Auth.RefreshSession;
using Lexora.Application.Auth.Services;
using Lexora.Application.Common.Options;
using Lexora.Domain.RefreshTokens;
using Lexora.Domain.Users;
using Lexora.Shared.Results;
using Lexora.TestKit;
using Lexora.TestKit.Builders;
using Lexora.TestKit.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Lexora.Application.UnitTests.Auth;

public sealed class AuthServiceRefreshTests
{
    private const string Password = "Str0ngPassword";

    private readonly FakeTimeProvider _time = new(TestClock.Now);
    private readonly FakePasswordHasher _hasher = new();
    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryRefreshTokenRepository _refreshTokens = new();
    private readonly AuthOptions _options = new();
    private readonly AuthService _service;
    private readonly User _user;

    public AuthServiceRefreshTests()
    {
        _service = new AuthService(
            _users,
            _refreshTokens,
            _hasher,
            new FakeAccessTokenIssuer(_time),
            new FakeSecretTokenGenerator(),
            new FakeCurrentUser(),
            Options.Create(_options),
            _time,
            NullLogger<AuthService>.Instance);

        _user = new UserBuilder().WithPasswordHash(_hasher.Hash(Password)).Build();
        _users.AddAsync(_user, CancellationToken.None);
    }

    [Fact]
    public async Task RefreshAsync_ActiveToken_RotatesItWithinTheSameSession()
    {
        var login = await SignInAsync();
        _time.Advance(TimeSpan.FromMinutes(20));

        var result = await RefreshAsync(login.RefreshToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.RefreshToken.ShouldNotBe(login.RefreshToken);
        result.Value.AccessToken.ShouldBe($"access-token:{_user.Id}");
        result.Value.RefreshTokenExpiresAt.ShouldBe(_time.GetUtcNow() + _options.RefreshTokenLifetime);
        result.Value.User.Id.ShouldBe(_user.Id);
        var previous = TokenFor(login.RefreshToken);
        previous.RevocationReason.ShouldBe(RefreshTokenRevocationReason.Rotated);
        TokenFor(result.Value.RefreshToken).FamilyId.ShouldBe(previous.FamilyId);
    }

    [Fact]
    public async Task RefreshAsync_TokenAlreadyRotated_RevokesTheWholeSessionFamily()
    {
        var login = await SignInAsync();
        var rotated = (await RefreshAsync(login.RefreshToken)).Value;

        var reuse = await RefreshAsync(login.RefreshToken);

        reuse.Notifications.ShouldBe([AuthNotifications.InvalidRefreshToken]);
        TokenFor(rotated.RefreshToken).RevocationReason.ShouldBe(RefreshTokenRevocationReason.ReuseDetected);
        (await RefreshAsync(rotated.RefreshToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task RefreshAsync_ReuseInOneSession_KeepsOtherSessionsOpen()
    {
        var first = await SignInAsync();
        var second = await SignInAsync();
        await RefreshAsync(first.RefreshToken);

        await RefreshAsync(first.RefreshToken);

        TokenFor(second.RefreshToken).IsActive(_time.GetUtcNow()).ShouldBeTrue();
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_FailsAsInvalid()
    {
        var login = await SignInAsync();
        _time.Advance(_options.RefreshTokenLifetime);

        (await RefreshAsync(login.RefreshToken)).Notifications.ShouldBe([AuthNotifications.InvalidRefreshToken]);
    }

    [Fact]
    public async Task RefreshAsync_TokenRevokedByLogout_FailsWithoutRevokingOtherTokens()
    {
        var login = await SignInAsync();
        TokenFor(login.RefreshToken).Revoke(RefreshTokenRevocationReason.Logout, _time.GetUtcNow());

        var result = await RefreshAsync(login.RefreshToken);

        result.Notifications.ShouldBe([AuthNotifications.InvalidRefreshToken]);
        TokenFor(login.RefreshToken).RevocationReason.ShouldBe(RefreshTokenRevocationReason.Logout);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown-token")]
    public async Task RefreshAsync_MissingOrUnknownToken_FailsAsInvalid(string? refreshToken) =>
        (await RefreshAsync(refreshToken)).Notifications.ShouldBe([AuthNotifications.InvalidRefreshToken]);

    [Fact]
    public async Task RefreshAsync_UserDeactivatedDuringTheSession_FailsAsInactiveAndClosesTheSession()
    {
        var login = await SignInAsync();
        _user.Deactivate(_time.GetUtcNow());

        var result = await RefreshAsync(login.RefreshToken);

        result.Notifications.ShouldBe([AuthNotifications.UserInactive]);
        TokenFor(login.RefreshToken).RevocationReason.ShouldBe(RefreshTokenRevocationReason.UserDeactivated);
    }

    private async Task<LoginResponse> SignInAsync() =>
        (await _service.LoginAsync(new LoginCommand(_user.Email.Value, Password), TestContext.Current.CancellationToken)).Value;

    private Task<Result<RefreshSessionResponse>> RefreshAsync(string? refreshToken) =>
        _service.RefreshAsync(new RefreshSessionCommand(refreshToken), TestContext.Current.CancellationToken);

    private RefreshToken TokenFor(string refreshToken) => _refreshTokens.Tokens.Single(token => token.TokenHash == $"sha256::{refreshToken}");
}
